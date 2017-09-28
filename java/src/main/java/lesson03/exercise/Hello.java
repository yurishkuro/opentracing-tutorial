package lesson03.exercise;

import io.opentracing.ActiveSpan;
import java.io.IOException;
import com.google.common.collect.ImmutableMap;
import com.uber.jaeger.Tracer;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.HttpUrl;
import lib.Tracing;

public class Hello {

    private final Tracer tracer;
    private final OkHttpClient client;

    private Hello(Tracer tracer) {
        this.tracer = tracer;
        this.client = new OkHttpClient();
    }

    private String getHttp(int port, String path, String param, String value) {
        try {
            HttpUrl url = new HttpUrl.Builder().scheme("http").host("localhost").port(port).addPathSegment(path)
                    .addQueryParameter(param, value).build();
            Request.Builder requestBuilder = new Request.Builder().url(url);
            Request request = requestBuilder.build();
            Response response = client.newCall(request).execute();
            if (response.code() != 200) {
                throw new RuntimeException("Bad HTTP result: " + response);
            }
            return response.body().string();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private void sayHello(String helloTo) {
        try (ActiveSpan span = tracer.buildSpan("say-hello").startActive()) {
            span.setTag("hello-to", helloTo);

            String helloStr = formatString(helloTo);
            printHello(helloStr);
        }
    }

    private String formatString(String helloTo) {
        try (ActiveSpan span = tracer.buildSpan("formatString").startActive()) {
            String helloStr = getHttp(8081, "format", "helloTo", helloTo);
            span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
            return helloStr;
        }
    }

    private void printHello(String helloStr) {
        try (ActiveSpan span = tracer.buildSpan("printHello").startActive()) {
            getHttp(8082, "publish", "helloStr", helloStr);
            span.log(ImmutableMap.of("event", "println"));
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
        System.exit(0); // okhttpclient sometimes hangs maven otherwise
    }
}
