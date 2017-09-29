# Lesson 4 - Baggage

## Objectives

* Understand distributed context propagation
* Use baggage to pass data through the call graph

### Walkthrough

In Lesson 3 we have seen how span context is propagated over the wire between different applications.
It is not hard to see that this process can be generalized to passing more than just the tracing context.
With OpenTracing instrumentation in place, we can support general purpose _distributed context propagation_
where we associate some metadata with the transaction and make that metadata available anywhere in the
distributed call graph. In OpenTracing this metadata is called _baggage_, to highlight the fact that
it is carried over in-band with all RPC requests, just like baggage.

To see how it works in OpenTracing, let's take the application we built in Lesson 3. You can copy the source
code from [../lesson03/solution](../lesson03/solution) package:

```
mkdir lesson04/exercise
cp -r lesson03/solution/*py lesson04/exercise/
```

The `formatter` service takes the `helloTo` parameter and returns a string `Hello, {helloTo}!`. Let's modify
it so that we can customize the greeting too, but without modifying the public API of that service.

### Set Baggage in the Client

Let's add/replace the following code to `hello.py`:

```python
assert len(sys.argv) == 3

tracer = init_tracer('hello-world')

hello_to = sys.argv[1]
greeting = sys.argv[2]
say_hello(hello_to, greeting)
```

And update `sayHello`:

```python
def say_hello(hello_to, greeting):
    with tracer.start_span('say-hello') as span:
        span.set_tag('hello-to', hello_to)
        span.set_baggage_item('greeting', greeting)
        with span_in_context(span):
            hello_str = format_string(hello_to)
            print_hello(hello_str)
```

By doing this we read a second command line argument as a "greeting" and store it in the baggage under `'greeting'` key.

### Read Baggage in Formatter

Change the following code in the `formatter`'s HTTP handler:

```python
with tracer.start_span('format', child_of=span_ctx, tags=span_tags) as span:
    greeting = span.get_baggage_item('greeting')
    if not greeting:
        greeting = 'Hello'
    hello_to = request.args.get('helloTo')
    return '%s, %s!' % (greeting, hello_to)
```

### Run it

As in Lesson 3, first start the `formatter` and `publisher` in separate terminals, then run the client
with two arguments, e.g. `Bryan Bonjour`. The `publisher` should print `Bonjour, Bryan!`.

```
# client
$ python -m lesson04.exercise.hello Bryan Bonjour
Initializing Jaeger Tracer with UDP reporter
Using sampler ConstSampler(True)
opentracing.tracer initialized to <jaeger_client.tracer.Tracer object at 0x10c172f50>[app_name=hello-world]
Starting new HTTP connection (1): localhost
http://localhost:8081 "GET /format?helloTo=Bryan HTTP/1.1" 200 15
Reporting span 1f5b4b5b21ea181d:821961d7d50eac1a:1f5b4b5b21ea181d:1 hello-world.format
Starting new HTTP connection (1): localhost
http://localhost:8082 "GET /publish?helloStr=Bonjour%2C+Bryan%21 HTTP/1.1" 200 9
Reporting span 1f5b4b5b21ea181d:214e6b2fb3400125:1f5b4b5b21ea181d:1 hello-world.println
Reporting span 1f5b4b5b21ea181d:1f5b4b5b21ea181d:0:1 hello-world.say-hello

# formatter
$ python -m lesson04.exercise.formatter
Initializing Jaeger Tracer with UDP reporter
Using sampler ConstSampler(True)
opentracing.tracer initialized to <jaeger_client.tracer.Tracer object at 0x10c7b0e90>[app_name=formatter]
 * Running on http://127.0.0.1:8081/ (Press CTRL+C to quit)
Reporting span 1f5b4b5b21ea181d:821961d7d50eac1a:1f5b4b5b21ea181d:1 formatter.format
127.0.0.1 - - [28/Sep/2017 23:35:04] "GET /format?helloTo=Bryan HTTP/1.1" 200 -

# publisher
$ python -m lesson04.exercise.publisher
Initializing Jaeger Tracer with UDP reporter
Using sampler ConstSampler(True)
opentracing.tracer initialized to <jaeger_client.tracer.Tracer object at 0x102c40e90>[app_name=publisher]
 * Running on http://127.0.0.1:8082/ (Press CTRL+C to quit)
Bonjour, Bryan!
Reporting span 1f5b4b5b21ea181d:214e6b2fb3400125:1f5b4b5b21ea181d:1 publisher.publish
127.0.0.1 - - [28/Sep/2017 23:35:04] "GET /publish?helloStr=Bonjour%2C+Bryan%21 HTTP/1.1" 200 -
```

### What's the Big Deal?

We may ask - so what, we could've done the same thing by passing the `greeting` as an HTTP request parameter.
However, that is exactly the point of this exercise - we did not have to change any APIs on the path from
the root span in `hello.py` all the way to the server-side span in `formatter`, three levels down.
If we had a much larger application with much deeper call tree, say the `formatter` was 10 levels down,
the exact code changes we made here would have worked, despite 8 more services being in the path.
If changing the API was the only way to pass the data, we would have needed to modify 8 more services
to get the same effect.

Some of the possible applications of baggage include:

  * passing the tenancy in multi-tenant systems
  * passing identity of the top caller
  * passing fault injection instructions for chaos engineering
  * passing request-scoped dimensions for other monitoring data, like separating metrics for prod vs. test traffic


### Now, a Warning... NOW a Warning?

Of course, while baggage is extermely powerful mechanism, it is also dangerous. If we store a 1Mb value/string
in baggage, every request in the call graph below that point will have to carry that 1Mb of data. So baggage
must be used with caution. In fact, Jaeger client libraries implement centrally controlled baggage restrictions,
so that only blessed services can put blessed keys in the baggage, with possible restrictions on the value length.

## Conclusion

The complete program can be found in the [solution](./solution) package. 
