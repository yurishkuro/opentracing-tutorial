package lesson03.solution;

import com.google.common.collect.ImmutableMap;
import io.dropwizard.Application;
import io.dropwizard.Configuration;
import io.dropwizard.setup.Environment;
import io.opentracing.Scope;
import io.opentracing.Span;
import io.opentracing.Tracer;
import lib.Tracing;

import javax.ws.rs.GET;
import javax.ws.rs.Path;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.HttpHeaders;
import javax.ws.rs.core.MediaType;

public class Formatter extends Application<Configuration> {
    private final Tracer tracer;

    private Formatter(Tracer tracer) {
        this.tracer = tracer;
    }

    @Path("/format")
    @Produces(MediaType.TEXT_PLAIN)
    public class FormatterResource {

        @GET
        public String format(@QueryParam("helloTo") String helloTo, @Context HttpHeaders httpHeaders) {
            Span span =  Tracing.startServerSpan(tracer, httpHeaders, "format");
            try (Scope scope = tracer.scopeManager().activate(span)) {
                String helloStr = String.format("Hello, %s!", helloTo);
                span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
                return helloStr;
            } finally {
                span.finish();
            }
        }

    }

    @Override
    public void run(Configuration configuration, Environment environment) throws Exception {
        environment.jersey().register(new FormatterResource());
    }

    public static void main(String[] args) throws Exception {
        System.setProperty("dw.server.applicationConnectors[0].port", "8081");
        System.setProperty("dw.server.adminConnectors[0].port", "9081");

        new Formatter(Tracing.init("formatter")).run(args);
    }
}
