'use strict';

var express = require('express')
var app = express()
var initTracer = require('../../lib/tracing').initTracer;

app.get('/', function (req, res) {
    var helloStr = sayHello(helloTo);
    res.send(helloStr)
})

function sayHello(helloTo) {
    var span = tracer.startSpan('say-hello');
    
    var ret = format_string(helloTo, span)[0];

    var helloStr = ret[0];

    span.log({
        'event': 'format-string',
        'value': helloStr
    });
    
    print_string(helloStr, ret[1]);
    span.log({'event': 'print-string'})
    
    span.finish();

    return helloStr;
}  

function format_string(helloTo, root_span) {
    var span = tracer.startSpan('format_string', {childOf: root_span.context()});
    var formattedStr = 'Hello, ' + helloTo + '!';
    span.finish();    
    return [formattedStr, span];
}

function print_string(helloStr, root_span) {
    var span = tracer.startSpan('print_string');
    console.log(helloStr);
    span.finish();
}
  
if (process.argv.length != 3) {
    throw new Error('expecting one argument')
}

var helloTo = process.argv[2];

var tracer = initTracer('hello-world');

app.listen(8080, function () {
    console.log('Hello app listening on port 8080')
})
