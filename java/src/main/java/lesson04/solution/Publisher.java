package lesson04.solution;

import javax.ws.rs.GET;
import javax.ws.rs.Path;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.HttpHeaders;
import javax.ws.rs.core.MediaType;

import com.google.common.collect.ImmutableMap;

import io.dropwizard.Application;
import io.dropwizard.Configuration;
import io.dropwizard.setup.Environment;
import io.jaegertracing.internal.JaegerTracer;
import io.opentracing.Scope;
import io.opentracing.Span;
import io.opentracing.Tracer;
import lib.Tracing;

public class Publisher extends Application<Configuration> {

    private final Tracer tracer;

    private Publisher(Tracer tracer) {
        this.tracer = tracer;
    }

    @Path("/publish")
    @Produces(MediaType.TEXT_PLAIN)
    public class PublisherResource {

        @GET
        public String format(@QueryParam("helloStr") String helloStr, @Context HttpHeaders httpHeaders) {
            Span span = Tracing.startServerSpan(tracer, httpHeaders, "publish");
            try (Scope scope = tracer.scopeManager().activate(span)) {
                System.out.println(helloStr);
                span.log(ImmutableMap.of("event", "println", "value", helloStr));
                return "published";
            } finally {
                span.finish();
            }
        }
    }

    @Override
    public void run(Configuration configuration, Environment environment) throws Exception {
        environment.jersey().register(new PublisherResource());
    }

    public static void main(String[] args) throws Exception {
        System.setProperty("dw.server.applicationConnectors[0].port", "8082");
        System.setProperty("dw.server.adminConnectors[0].port", "9082");
        try (JaegerTracer tracer = Tracing.init("publisher")) {
            new Publisher(tracer).run(args);
        }
    }
}
