const assert = require("assert");
const { initTracer } = require("../../lib/tracing");

const tracer = initTracer("hello-world");

const sayHello = helloTo => {
  const span = tracer.startSpan("say-hello");
  span.setTag("hello-to", helloTo);
  const helloStr = formatString(span, helloTo);
  printString(span, helloStr);
  span.finish();
};

const formatString = (rootSpan, helloTo) => {
  const span = tracer.startSpan("format", { childOf: rootSpan.context() });
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  span.finish();
  return helloStr;
};

const printString = (rootSpan, helloStr) => {
  const span = tracer.startSpan("consoleLog", { childOf: rootSpan.context() });
  console.log(helloStr);
  span.log({ event: "print-string" });
  span.finish();
};

assert.ok(process.argv.length == 3, "Expecting one argument");
const helloTo = process.argv[2];

sayHello(helloTo);

tracer.close(() => process.exit());
