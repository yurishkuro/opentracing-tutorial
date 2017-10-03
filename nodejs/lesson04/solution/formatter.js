'use strict';

const express = require('express')
const app = express()
const initTracer = require('../../lib/tracing').initTracer;
const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

const tracer = initTracer('format-service');

const port = 8081;

app.listen(port, function () {
    console.log('Formatter app listening on port ' + port);
})

app.get('/format/:str', function (req, res) {
    const parentSpanContext = tracer.extract(FORMAT_HTTP_HEADERS, req.headers)
    const span = tracer.startSpan('http_server', {
        childOf: parentSpanContext
    });
    span.setTag(Tags.SPAN_KIND, Tags.SPAN_KIND_RPC_SERVER);

    const str= req.params.str;

    span.log({
        'event': 'format',
        'value': str
    });

    //this requires downstream service to know about the baggage content
    let greeting = span.getBaggageItem('greeting') || 'Hello';

    span.finish();

    res.send(`${greeting}, ${str}`);
})



