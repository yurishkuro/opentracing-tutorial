const assert = require("assert");
const initJaegerTracer = require("jaeger-client").initTracer;

const initTracer = serviceName => {
  const config = {
    serviceName: serviceName,
    sampler: {
      type: "const",
      param: 1,
    },
    reporter: {
      logSpans: true,
    },
  };
  const options = {
    logger: {
      info: function logInfo(msg) {
        console.log("INFO ", msg);
      },
      error: function logError(msg) {
        console.log("ERROR", msg);
      },
    },
  };
  return initJaegerTracer(config, options);
};

const tracer = initTracer("hello-world");

const sayHello = helloTo => {
  const span = tracer.startSpan("say-hello");
  span.setTag("hello-to", helloTo);
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  console.log(helloStr);
  span.log({ event: "print-string" });
  span.finish();
};

assert.ok(process.argv.length == 3, "Expecting one argument");
const helloTo = process.argv[2];

sayHello(helloTo);
