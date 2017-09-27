package lib;

import com.uber.jaeger.Configuration;
import com.uber.jaeger.Configuration.ReporterConfiguration;
import com.uber.jaeger.Configuration.SamplerConfiguration;
import io.opentracing.ActiveSpan;
import io.opentracing.SpanContext;
import io.opentracing.Tracer;
import io.opentracing.propagation.Format;
import io.opentracing.propagation.TextMapExtractAdapter;
import java.util.HashMap;
import javax.ws.rs.core.MultivaluedMap;

public final class Tracing {
    private Tracing() {
    }

    public static com.uber.jaeger.Tracer init(String service) {
        SamplerConfiguration samplerConfig = new SamplerConfiguration("const", 1);
        ReporterConfiguration reporterConfig = new ReporterConfiguration(true, null, null, null, null);
        Configuration config = new Configuration(service, samplerConfig, reporterConfig);
        return (com.uber.jaeger.Tracer) config.getTracer();
    }

    public static ActiveSpan startServerSpan(Tracer tracer, javax.ws.rs.core.HttpHeaders httpHeaders,
            String operationName) {
        // format the headers for extraction
        MultivaluedMap<String, String> rawHeaders = httpHeaders.getRequestHeaders();
        final HashMap<String, String> headers = new HashMap<String, String>();
        for (String key : rawHeaders.keySet()) {
            headers.put(key, rawHeaders.get(key).get(0));
        }

        ActiveSpan span;
        try {
            SpanContext parentSpan = tracer.extract(Format.Builtin.HTTP_HEADERS, new TextMapExtractAdapter(headers));
            if (parentSpan == null) {
                span = tracer.buildSpan(operationName).startActive();
            } else {
                span = tracer.buildSpan(operationName).asChildOf(parentSpan).startActive();
            }
        } catch (IllegalArgumentException e) {
            span = tracer.buildSpan(operationName).startActive();
        }
        return span;
    }
}