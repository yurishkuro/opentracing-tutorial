package lesson02.solution;

import io.opentracing.ActiveSpan;
import io.opentracing.util.GlobalTracer;

import com.google.common.collect.ImmutableMap;
import com.uber.jaeger.Tracer;
import lib.Tracing;

public class HelloActive {
    
    private final Tracer tracer;

    private HelloActive(Tracer tracer) {
        this.tracer = tracer;
    }

    private void sayHello(String helloTo) {
        try (ActiveSpan span = tracer.buildSpan("say-hello").startActive()) {
            span.setTag("hello-to", helloTo);
            
            String helloStr = formatString(helloTo);
            printHello(helloStr);
        }
    }

    private  String formatString(String helloTo) {
        try (ActiveSpan span = tracer.buildSpan("formatString").startActive()) {
            String helloStr = String.format("Hello, %s!", helloTo);
            span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
            return helloStr;
        }
    }

    private void printHello(String helloStr) {
        try (ActiveSpan span = tracer.buildSpan("printHello").startActive()) {
            System.out.println(helloStr);
            span.log(ImmutableMap.of("event", "println"));
        }
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        Tracer tracer = Tracing.init("hello-world");
        new HelloActive(tracer).sayHello(helloTo);
        tracer.close();
    }
}
