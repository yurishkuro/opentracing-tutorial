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

//wait for udp message sent out
setTimeout( e => {tracer.close();}, 12000);

/* another way to make sure udp message is sent out is by listening to the message event
const server = require('dgram').createSocket('udp4');

server.on('message', (msg, rinfo) => {
  tracer.close();
  server.close();
});

server.bind(6832);
*/


