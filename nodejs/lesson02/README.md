---
**NOTE**

This README is currently incomplete / unfinished. Please refer to respective README in tutorials for one of the other languages

---

# Lesson 2 - Context and Tracing Functions

## Objectives

Learn how to:

* Trace individual functions
* Combine multiple spans into a single trace
* Propagate the in-process context

## Walkthrough

### A simple Hello-World program

```

Run it:
```
npm install
node lesson02/solution/hello.js Peter
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080

```

```

You should see something below on the console for the client app:

```
INFO  Reporting span d6c674cb77b1ba9c:933a7b0d05ed1b48:d6c674cb77b1ba9c:1
Hello, Peter!
INFO  Reporting span d6c674cb77b1ba9c:263fe6d3cb907321:d6c674cb77b1ba9c:1
INFO  Reporting span d6c674cb77b1ba9c:d6c674cb77b1ba9c:0:1
```