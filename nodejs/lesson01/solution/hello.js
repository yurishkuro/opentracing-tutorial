const assert = require("assert");
const { initTracer } = require("../../lib/tracing");

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

const closeTracer = () => tracer.close();

setTimeout(closeTracer, 12000);
