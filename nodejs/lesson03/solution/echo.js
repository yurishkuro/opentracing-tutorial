'use strict';

var express = require('express')
var app = express()
var initTracer = require('../../lib/tracing').initTracer;
const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

app.get('/', function (req, res) {
    const parentSpanContext = tracer.extract(FORMAT_HTTP_HEADERS, req.headers)
    const span = tracer.startSpan('http_server', {
        childOf: parentSpanContext
    })

    var helloStr = sayHello(helloTo, span);
    res.send(helloStr)
})

function sayHello(helloTo, span) {
    
    var helloStr = format_string(helloTo, span);

    span.log({
        'event': 'sayHello',
        'value': helloStr
    });
    
    print_string(helloStr, span);
    span.log({'event': 'print-string'})
    
    span.finish();

    return helloStr;
}  

function format_string(helloTo, root_span) {
    var span = tracer.startSpan('format_string', {childOf: root_span.context()});
    var formattedStr = 'Hello, ' + helloTo + '!';

    span.log({
        'event': 'format-string',
        'value': helloTo
    });

    span.finish();    
    return formattedStr;
}

function print_string(helloStr, root_span) {
    var span = tracer.startSpan('print_string', {childOf: root_span.context()});
    span.log({
        'event': 'print-string',
        'value': helloStr
    });
    console.log(helloStr);
    span.finish();
}
  
if (process.argv.length != 3) {
    throw new Error('expecting one argument')
}

var helloTo = process.argv[2];

var tracer = initTracer('hello-world');

app.listen(8081, function () {
    console.log('Hello app listening on port 8081')
})
