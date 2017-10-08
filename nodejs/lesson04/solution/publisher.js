'use strict';

const express = require('express')
const app = express()
const initTracer = require('../../lib/tracing').initTracer;
const { Tags, FORMAT_HTTP_HEADERS } = require('opentracing');

const tracer = initTracer('publish-service');

const port = 8082;

app.listen(port, function () {
    console.log('Publisher app listening on port ' + port);
})

app.get('/publish', function (req, res) {
    const parentSpanContext = tracer.extract(FORMAT_HTTP_HEADERS, req.headers)
    const span = tracer.startSpan('http_server', {
        childOf: parentSpanContext
    });
    span.setTag(Tags.SPAN_KIND, Tags.SPAN_KIND_RPC_SERVER);

    const str= req.query.helloStr;

    span.log({
        'event': 'publish',
        'value': str
    });

    console.log(str);

    span.finish();

    res.send('published!');
})



