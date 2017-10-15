# Lesson 4 - Baggage

## Objectives

* Understand distributed context propagation
* Use baggage to pass data through the call graph


## Walkthrough

### Hello-World Microservice App


To test it out, run the formatter and publisher services in separate terminals

```
# terminal 1
$ node lesson03/solution/formatter.js
 * Running on http://localhost:8081/ (Press CTRL+C to quit)

# terminal 3
$ node lesson03/solution/publisher.js
 * Running on http://localhost:8082/ (Press CTRL+C to quit)
```


Finally, if we run the client app as we did in the previous lessons:

```
$ node lesson03/solution/hello.js Peter Hello
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080
INFO  Reporting span b9b1b4d5b39165f0:c2af04baeb8fba28:b9b1b4d5b39165f0:1
INFO  Reporting span b9b1b4d5b39165f0:38631f53c6829d3a:b9b1b4d5b39165f0:1
INFO  Reporting span b9b1b4d5b39165f0:b9b1b4d5b39165f0:0:1

```

On the formatter terminal screen:
```
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Formatter app listening on port 8081
INFO  Reporting span b9b1b4d5b39165f0:2f647091ec9d7011:c2af04baeb8fba28:1
```

On the publisher terminal screen:
```
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Publisher app listening on port 8082
Hello, Peter
INFO  Reporting span b9b1b4d5b39165f0:a07ba68f7d798215:38631f53c6829d3a:1
```

### hello-context.js 
As you can see in all of the lessons so far, introducing Opentracing functionality in the application code does make the code cluttered a bit, especially requires us to pass the span object around if we need to establish parent-child relationship between spans. In this example, we use one of the latest Node.js experimental feature called 'Async Hook' (https://nodejs.org/api/async_hooks.html), which allows us to register callbacks for the asynchronous resources, such as in our case the Promise object. We use a Node.js module called "continuation-local-storage" to establish the context for the request Promise chain. 

