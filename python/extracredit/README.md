# Extra Credit - Using Existing Open Source Instrumentation

Adding manual instrumentation to `flask` and `requests` like we did in [Lesson 3](../lesson03)
is tedious. Fortunately, we don't need to do that because that instrumentation itself already exists
as open source modules. For an extra credit, try to use these libraries to avoid instrumenting your
code manually:

 * https://github.com/uber-common/opentracing-python-instrumentation
 * https://github.com/opentracing-contrib/python-flask

There is another tutorial specifically for the Flask framework: http://blog.scoutapp.com/articles/2018/01/15/tutorial-tracing-python-flask-requests-with-opentracing
