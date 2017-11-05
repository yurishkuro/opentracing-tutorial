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

app.get('/format', function (req, res) {
    const parentSpanContext = tracer.extract(FORMAT_HTTP_HEADERS, req.headers)
    const span = tracer.startSpan('http_server', {
        childOf: parentSpanContext,
        tags: {[Tags.SPAN_KIND]: Tags.SPAN_KIND_RPC_SERVER}
    });

    const helloTo= req.query.helloTo;

    span.log({
        'event': 'format',
        'value': helloTo
    });

    span.finish();

    res.send(`Hello, ${helloTo}!`);
})



