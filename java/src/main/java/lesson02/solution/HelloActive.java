package lesson02.solution;

import com.google.common.collect.ImmutableMap;

import io.opentracing.Scope;
import io.opentracing.Span;
import io.opentracing.Tracer;
import lib.Tracing;

public class HelloActive {

    private final Tracer tracer;

    private HelloActive(Tracer tracer) {
        this.tracer = tracer;
    }

    private void sayHello(String helloTo) {
        Span span = tracer.buildSpan("say-hello").start();
        try (Scope scope = tracer.scopeManager().activate(span)) {
            span.setTag("hello-to", helloTo);

            String helloStr = formatString(helloTo);
            printHello(helloStr);
        } finally{
            span.finish();
        }
    }

    private  String formatString(String helloTo) {
        Span span = tracer.buildSpan("formatString").start();
        try (Scope scope = tracer.scopeManager().activate(span)) {
            String helloStr = String.format("Hello, %s!", helloTo);
            span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
            return helloStr;
        } finally{
            span.finish();
        }
    }

    private void printHello(String helloStr) {
        Span span = tracer.buildSpan("printHello").start();
        try (Scope scope = tracer.scopeManager().activate(span)) {
            System.out.println(helloStr);
            span.log(ImmutableMap.of("event", "println"));
        } finally{
            span.finish();
        }
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }

        String helloTo = args[0];
        try (Tracer tracer = Tracing.init("hello-world")) {
            new HelloActive(tracer).sayHello(helloTo);
        }
    }
}
