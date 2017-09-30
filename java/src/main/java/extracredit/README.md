# Extra Credit - Using Existing Open Source Instrumentation

Adding manual instrumentation to Dropwizard and okhttp like we did in [Lesson 3](../lesson03)
is tedious. Fortunately, we don't need to do that because that instrumentation itself already exists
as open source modules:

  * https://github.com/opentracing-contrib/java-dropwizard
  * https://github.com/opentracing-contrib/java-okhttp

For an extra credit, try to use these modules to avoid instrumenting your code manually.

