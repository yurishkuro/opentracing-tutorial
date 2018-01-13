package lesson03.solution;

import com.google.common.collect.ImmutableMap;

import io.dropwizard.Application;
import io.dropwizard.Configuration;
import io.dropwizard.setup.Environment;
import io.opentracing.Scope;
import io.opentracing.Tracer;

import javax.ws.rs.GET;
import javax.ws.rs.Path;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.HttpHeaders;
import javax.ws.rs.core.MediaType;

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
            try (Scope scope = Tracing.startServerSpan(tracer, httpHeaders, "publish")) {
                System.out.println(helloStr);
                scope.span().log(ImmutableMap.of("event", "println", "value", helloStr));
                return "published";
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
        Tracer tracer = Tracing.init("publisher");
        new Publisher(tracer).run(args);
    }
}
