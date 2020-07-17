'use strict';

const assert = require('assert');
const initTracer = require('../../lib/tracing').initTracer;
const request = require('request-promise');
const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

function sayHello(helloTo, greeting) {

    const span = tracer.startSpan('say-hello');
    span.setTag('hello-to', helloTo);

    span.setBaggageItem('greeting', greeting)

    format_string(helloTo, span)
        .then( data => {
            return print_hello(data, span);
        })
        .then( data => {
            span.setTag(Tags.HTTP_STATUS_CODE, 200)
            span.finish();
        })
        .catch( err => {
            span.setTag(Tags.ERROR, true) 
            span.setTag(Tags.HTTP_STATUS_CODE, err.statusCode || 500);
            span.finish();
            throw err;
        });

}

function format_string(input, root_span) {
    const url = `http://localhost:8081/format?helloTo=${input}`;
    const fn = 'format';

    const span = tracer.startSpan(fn, {childOf: root_span.context()});
    span.log({
        'event': 'format-string',
        'value': input
    });

    return http_get(url, span); 
}  

function print_hello(input, root_span) {
    const url = `http://localhost:8082/publish?helloStr=${input}`;
    const fn = 'publish';

    const span = tracer.startSpan(fn, {childOf: root_span.context()});
    span.log({
        'event': 'print-string',
        'value': input
    });
    return http_get(url, span);
}

function http_get(url, span) {
    const method = 'GET';
    const headers = {};
    
    span.setTag(Tags.HTTP_URL, url);
    span.setTag(Tags.HTTP_METHOD, method);
    span.setTag(Tags.SPAN_KIND, Tags.SPAN_KIND_RPC_CLIENT);
    // Send span context via request headers (parent id etc.)
    tracer.inject(span, FORMAT_HTTP_HEADERS, headers);

    return request({url, method, headers})
            .then( data => {
                span.finish();
                return data;
            }, e => {
                span.setTag(Tags.ERROR, true)
                span.log({
                    'event': 'error',
                    'error.object': e
                });
                span.finish();
                throw e;
            });

}

assert.ok(process.argv.length == 4, 'expecting two argument');

const [, , helloTo, greeting] = process.argv;

const tracer = initTracer('hello-world');

sayHello(helloTo, greeting);

setTimeout( e => {tracer.close();}, 12000);
