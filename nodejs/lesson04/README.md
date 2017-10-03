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
$ node lesson03/solution/hello.js
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080

```

Run the following curl command a few times:

```
curl localhost:8080/peter
```

You should see something below on the console for the client app:

```
INFO  Reporting span 18fbea3958bcf3c9:fb3c19ff97981c3:18fbea3958bcf3c9:1
INFO  Reporting span 18fbea3958bcf3c9:d7d455d64d6c22ff:18fbea3958bcf3c9:1
INFO  Reporting span 18fbea3958bcf3c9:18fbea3958bcf3c9:0:1
INFO  Reporting span 984b77dfbe0df281:f5e02d55e8c5005c:984b77dfbe0df281:1
INFO  Reporting span 984b77dfbe0df281:cd7468450b4560a9:984b77dfbe0df281:1
INFO  Reporting span 984b77dfbe0df281:984b77dfbe0df281:0:1
INFO  Reporting span 44c88c2d1b036968:5cdd4334d1d459e7:44c88c2d1b036968:1
INFO  Reporting span 44c88c2d1b036968:316264993822da2f:44c88c2d1b036968:1
INFO  Reporting span 44c88c2d1b036968:44c88c2d1b036968:0:1
```