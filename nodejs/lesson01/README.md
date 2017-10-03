# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

```

Run it:
```
npm install
node lesson01/solution/hello.js Peter
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080

```

Run the following curl command a few times:

```
curl localhost:8080
```

You should see something below on the console for the client app:

```
Hello, Peter!
INFO  Reporting span 6d8e165388a35fb5:6d8e165388a35fb5:0:1
Hello, Peter!
INFO  Reporting span 48b662d422dfcc86:48b662d422dfcc86:0:1
Hello, Peter!
INFO  Reporting span c0e45d92229168c5:c0e45d92229168c5:0:1
```