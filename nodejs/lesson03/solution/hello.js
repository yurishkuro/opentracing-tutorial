'use strict';

var express = require('express')
var app = express()
var initTracer = require('../../lib/tracing').initTracer;

const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

var request = require('request');

app.get('/', function (req, res) {
    let url = 'localhost:8081', method = 'GET';

    var helloStr = request(url, method, helloTo);
    res.send(helloStr)
})

app.get('/lesson03', function (req, res){

// Request options
const uri = 'http://localhost:8081'
const method = 'GET'
const headers = {}
// Start a span
const span = tracer.startSpan('http_request')
span.setTag(Tags.HTTP_URL, uri)
span.setTag(Tags.HTTP_METHOD, method)

// Send span context via request headers (parent id etc.)
tracer.inject(span, FORMAT_HTTP_HEADERS, headers)

request({ uri, method, headers }, (err, data) => {
    console.log(err, data);

  // Error handling
  if (err) {
    span.setTag(Tags.ERROR, true)
    span.setTag(Tags.HTTP_STATUS_CODE, err.statusCode)
    span.log({
      event: 'error',
      message: err.message,
      err
    })
    span.finish()
    res.send('error')
    return
  }

  // Finish span
  span.setTag(Tags.HTTP_STATUS_CODE, data.statusCode)
  span.finish()
  res.send('ok')
})


})

function sayHello(helloTo) {
    var span = tracer.startSpan('say-hello');
    
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

app.listen(8080, function () {
    console.log('Hello app listening on port 8080')
})
