# Lesson 4 - Baggage

## Objectives

* Understand distributed context propagation
* Use baggage to pass data through the call graph

### Walkthrough

In Lesson 3 we have seen how span context is propagated over the wire between different applications.
It is not hard to see that this process can be generalized to propagating more than just the tracing context.
With OpenTracing instrumentation in place, we can support general purpose _distributed context propagation_
where we associate some metadata with the transaction and make that metadata available anywhere in the
distributed call graph. In OpenTracing this metadata is called _baggage_, to highlight the fact that
it is carried over in-band with all RPC requests, just like baggage.

To see how it works in OpenTracing, let's take the application we built in Lesson 3. You can copy the source
code from [../lesson03/solution](../lesson03/solution) package:

```
cp -r ./lesson03/solution ./lesson04/exercise
```

The `formatter` service takes the `helloTo` parameter and returns a string `Hello, {helloTo}!`. Let's modify
it so that we can customize the greeting too, but without modifying the public API of that service.

### Set Baggage in the Client

Let's add/replace the following code to `client/hello.go`:

```go
if len(os.Args) != 3 {
    panic("ERROR: Expecting two arguments")
}

greeting := os.Args[2]

// after starting the span
span.SetBaggageItem("greeting", greeting)
```

Here we read a second command line argument as a "greeting" and store it in the baggage under `"greeting"` key.

### Read Baggage in Formatter

Add the following code to the `formatter`'s HTTP handler:

```go
greeting := span.BaggageItem("greeting")
if greeting == "" {
    greeting = "Hello"
}

helloTo := r.FormValue("helloTo")
helloStr := fmt.Sprintf("%s, %s!", greeting, helloTo)
```

### Run it

As in Lesson 3, first start the `formatter` and `publisher` in separate terminals, then run the client
with two arguments, e.g. `hello.go Bryan Bonjour`. The `publisher` should print `Bonjour, Bryan!`.

```
# client
$ go run ./lesson04/exercise/client/hello.go Bryan Bonjour
2017/09/25 17:44:02 Initializing logging reporter
2017/09/25 17:44:02 Reporting span 719c2d0b77869cb3:536316b3383042c8:719c2d0b77869cb3:1
2017/09/25 17:44:02 Reporting span 719c2d0b77869cb3:4970dc35776b6b8a:719c2d0b77869cb3:1
2017/09/25 17:44:02 Reporting span 719c2d0b77869cb3:719c2d0b77869cb3:0:1

# formatter
$ go run ./lesson04/exercise/formatter/formatter.go
2017/09/25 17:43:43 Initializing logging reporter
2017/09/25 17:44:02 Reporting span 719c2d0b77869cb3:79e7ce843e340d93:536316b3383042c8:1

# publisher
$ go run ./lesson04/exercise/publisher/publisher.go
2017/09/25 17:43:46 Initializing logging reporter
Bonjour, Bryan!
2017/09/25 17:44:02 Reporting span 719c2d0b77869cb3:64846658fbbf5e3e:4970dc35776b6b8a:1
```

### What's the Big Deal?

We may ask - so what, we could've done the same thing by passing the `greeting` as an HTTP request parameter.
However, that is exactly the point of this exercise - we did not have to change any APIs on the path from
the root span in `hello.go` all the way to the server-side span in `formatter`, three levels down.
If we had a much larger application with much deeper call tree, say the `formatter` was 10 levels down,
the exact code changes we made here would have worked, despite 8 more services being in the path.
If changing the API was the only way to pass the data, we would have needed to modify 8 more services
to get the same effect.

Some of the possible applications of baggage include:

  * passing the tenancy in multi-tenant systems
  * passing security tokens
  * passing fault injection instructions for chaos engineering
  * passing request-scoped dimensions for other monitoring data, like separating metrics for prod vs. test traffic


### Now, a Warning... NOW a Warning?

Of course, while baggage is extermely powerful mechanism, it is also dangerous. If we store a 1Mb value/string
in baggage, every request in the call graph below that point will have to carry that 1Mb of data. So baggage
must be used with caution. In fact, Jaeger client libraries implement centrally controlled baggage restrictions,
so that only blessed services can put blessed keys in the baggage, with possible restrictions on the value length.

## Conclusion

The complete program can be found in the [solution](./solution) package. 
