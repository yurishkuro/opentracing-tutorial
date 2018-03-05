package lesson02.exercise;

import io.opentracing.Span;
import com.google.common.collect.ImmutableMap;
import com.uber.jaeger.Tracer;
import lib.Tracing;

public class Hello {

    private final Tracer tracer;

    private Hello(Tracer tracer) {
        this.tracer = tracer;
    }

    private void sayHello(String helloTo) {
        Span span = tracer.buildSpan("say-hello").startManual();
        span.setTag("hello-to", helloTo);
        
        String helloStr = formatString(span, helloTo);
        printHello(span, helloStr);

        span.finish();
    }

    private  String formatString(Span rootSpan, String helloTo) {
        Span span = tracer.buildSpan("formatString").startManual();
        try {
            String helloStr = String.format("Hello, %s!", helloTo);
            span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
            return helloStr;
        } finally {
            span.finish();
        }
    }

    private void printHello(Span rootSpan, String helloStr) {
        Span span = tracer.buildSpan("printHello").startManual();
        try {
            System.out.println(helloStr);
            span.log(ImmutableMap.of("event", "println"));
        } finally {
            span.finish();
        }
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        Tracer tracer = Tracing.init("hello-world");
        new Hello(tracer).sayHello(helloTo);
        tracer.close();
    }
}
