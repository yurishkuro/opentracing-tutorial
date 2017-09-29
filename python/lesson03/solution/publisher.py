from flask import Flask
from flask import request
from lib.tracing import init_tracer
from opentracing.ext import tags
from opentracing.propagation import Format

app = Flask(__name__)
tracer = init_tracer('publisher')

@app.route("/publish")
def publish():
    span_ctx = tracer.extract(Format.HTTP_HEADERS, request.headers)
    span_tags = {tags.SPAN_KIND: tags.SPAN_KIND_RPC_SERVER}
    with tracer.start_span('publish', child_of=span_ctx, tags=span_tags):
        hello_str = request.args.get('helloStr')
        print(hello_str)
        return 'published'

if __name__ == "__main__":
    app.run(port=8082)
