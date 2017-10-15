# Lesson 3 - Tracing RPC Requests

## Objectives

Learn how to:

* Trace a transaction across more than one microservice
* Pass the context between processes using `Inject` and `Extract`
* Apply OpenTracing-recommended tags

## Walkthrough

### Hello-World Microservice App

To test it out, first run the formatter and publisher services in separate terminals

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
$ node lesson03/solution/hello.js Peter
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080
INFO  Reporting span 80c31f112061d86e:1def63dfd6d6755d:80c31f112061d86e:1
INFO  Reporting span 80c31f112061d86e:e47ca83f948cb0c4:80c31f112061d86e:1
INFO  Reporting span 80c31f112061d86e:80c31f112061d86e:0:1
```

On formatter terminal screen: 
```
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Formatter app listening on port 8081
INFO  Reporting span 80c31f112061d86e:9ad7c16acf5f0994:1def63dfd6d6755d:1
```

On publisher terminal screen:
```
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Publisher app listening on port 8082
Hello, Peter!
INFO  Reporting span 80c31f112061d86e:f3211e5bb77c5f2b:e47ca83f948cb0c4:1
```
