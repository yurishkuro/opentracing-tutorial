'use strict';

const express = require('express')
const app = express()
const initTracer = require('../../lib/tracing').initTracer;
var request = require('request-promise');

const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

const tracer = initTracer('hello-world');

const port = 8080;

app.listen(port, function () {
    console.log('Hello app listening on port ' + port);
})

// main endpoint
app.get('/:str', main);

function main(req, res) {
    const input = req.params.str;

    const span = tracer.startSpan('say-hello-request');

    span.log({
        'event': 'sayHelloRequest',
        'value': input
    });

    format_string(input, span)
        .then( data => {
            return print_hello(data, span);
        })
        .then( data => {
            span.setTag(Tags.HTTP_STATUS_CODE, 200)
            span.finish()
            res.send('Got response back from service: ' + data);
        })
        .catch( err => {
            console.error(err.message);
            span.setTag(Tags.ERROR, true)
            span.setTag(Tags.HTTP_STATUS_CODE, err.statusCode || 500);
            span.finish();
            res.send('Sorry, an error happened during call downstream service!');
        });

}

function format_string(input, root_span) {
    const url = 'http://localhost:8081/format/' + input;
    const fn = 'format';

    const span = tracer.startSpan(fn, {childOf: root_span.context()});
    span.log({
        'event': 'format-string',
        'value': input
    });

    return http_get(fn, url, span); 
}  

function print_hello(input, root_span) {
    const url = 'http://localhost:8082/publish/' + input;
    const fn = 'publish';

    const span = tracer.startSpan(fn, {childOf: root_span.context()});
    span.log({
        'event': 'print-string',
        'value': input
    });
    return http_get(fn, url, span);
}

function http_get(fn, url, span) {
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
            });

}

  

