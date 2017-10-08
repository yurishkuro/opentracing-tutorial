'use strict';

var assert = require('assert');
var initTracer = require('../../lib/tracing').initTracer;

function sayHello(helloTo) {
    var span = tracer.startSpan('say-hello');
    span.setTag('hello-to', helloTo);

    var helloStr = `Hello, ${helloTo}!`;
    span.log({
        'event': 'string-format',
        'value': helloStr
    });
    
    console.log(helloStr);

    span.log({'event': 'print-string'})
    span.finish();
}  
  
assert.ok(process.argv.length == 3, 'expecting one argument');

var helloTo = process.argv[2];

var tracer = initTracer('hello-world');

sayHello(helloTo);

//flush out the span and close any reporters and senders
tracer.close();

