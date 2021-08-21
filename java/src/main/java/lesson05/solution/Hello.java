package lesson05.solution;

import com.google.common.collect.ImmutableMap;
import io.jaegertracing.internal.JaegerTracer;
import io.opentracing.Scope;
import io.opentracing.Tracer;
import io.opentracing.contrib.okhttp3.TracingCallFactory;
import lib.Tracing;
import okhttp3.Call;
import okhttp3.HttpUrl;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;

import java.io.IOException;

public class Hello {

    private final Tracer       tracer;
    private final Call.Factory traceClient;

    private Hello(Tracer tracer) {
        this.tracer = tracer;
        traceClient = new TracingCallFactory(new OkHttpClient(), tracer);
    }

    private String getHttp(int port, String path, String param, String value) {
        try {
            HttpUrl url = new HttpUrl.Builder().scheme("http").host("localhost").port(port).addPathSegment(path)
                    .addQueryParameter(param, value).build();
            Request.Builder requestBuilder = new Request.Builder().url(url);
            Request request = requestBuilder.build();
            Response response = traceClient.newCall(request).execute();
            tracer.activeSpan().setTag("invoked", "okhttptracer");
            if (response.code() != 200) {
                throw new RuntimeException("Bad HTTP result: " + response);
            }
            return response.body().string();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private void sayHello(String helloTo, String greeting) {
        try (Scope scope = tracer.buildSpan("say-hello").startActive(true)) {
            scope.span().setTag("hello-to", helloTo);
            scope.span().setBaggageItem("greeting", greeting);

            String helloStr = formatString(helloTo);
            printHello(helloStr);
        }
    }

    private String formatString(String helloTo) {
        try (Scope scope = tracer.buildSpan("formatString").startActive(true)) {
            String helloStr = getHttp(8081, "format", "helloTo", helloTo);
            scope.span().log(ImmutableMap.of("event", "string-format", "value", helloStr));
            return helloStr;
        }
    }

    private void printHello(String helloStr) {
        try (Scope scope = tracer.buildSpan("printHello").startActive(true)) {
            getHttp(8082, "publish", "helloStr", helloStr);
            scope.span().log(ImmutableMap.of("event", "println"));
        }
    }

    public static void main(String[] args) {
        if (args.length != 2) {
            throw new IllegalArgumentException("Expecting two arguments, helloTo and greeting");
        }
        String helloTo = args[0];
        String greeting = args[1];
        try (JaegerTracer tracer = Tracing.init("hello-world")) {
            new Hello(tracer).sayHello(helloTo, greeting);
        }
    }
}
