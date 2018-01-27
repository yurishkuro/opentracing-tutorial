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

//tracer.close(callback gets called after all work is complete.  Likely need to use process.exit node call)

// tracer.close(function() {
//   process.exit();
// });
//Note: the above close call just closes right away, like it is sync, not async.


const closeTracer = () => tracer.close();

setTimeout(closeTracer, 12000);
