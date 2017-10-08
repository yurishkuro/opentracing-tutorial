'use strict';

const assert = require('assert');
const initTracer = require('../../lib/tracing').initTracer;

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

const helloTo = process.argv[2];

const tracer = initTracer('hello-world');

sayHello(helloTo);

//flush out the span and close any reporters and senders
tracer.close();

