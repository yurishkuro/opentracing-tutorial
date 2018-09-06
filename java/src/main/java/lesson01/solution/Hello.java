package lesson01.solution;

import com.google.common.collect.ImmutableMap;
import com.uber.jaeger.Tracer;

import io.opentracing.Span;
import lib.Tracing;

public class Hello {

    private final Tracer tracer;

    private Hello(Tracer tracer) {
        this.tracer = tracer;
    }

    private void sayHello(String helloTo) {
        Span span = tracer.buildSpan("say-hello").start();
        span.setTag("hello-to", helloTo);

        String helloStr = String.format("Hello, %s!", helloTo);
        span.log(ImmutableMap.of("event", "string-format", "value", helloStr));

        System.out.println(helloStr);
        span.log(ImmutableMap.of("event", "println"));

        span.finish();
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        try (Tracer tracer = Tracing.init("hello-world")) {
            new Hello(tracer).sayHello(helloTo);
        }
    }
}
