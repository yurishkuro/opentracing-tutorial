# Extra Credit - Using Existing Open Source Instrumentation

Adding manual instrumentation to `flask` and `requests` like we did in [Lesson 3](../lesson03)
is tedious. Fortunately, we don't need to do that because that instrumentation already exists
as open source modules. For extra credit, use these libraries to avoid instrumenting your
code manually:

 * [opentracing-python-instrumentation](https://github.com/uber-common/opentracing-python-instrumentation) - instruments requests, SQLAlchemy, redis, and more.
 * [python-flask](https://github.com/opentracing-contrib/python-flask) - automatically instruments web requests to Flask apps. [This tutorial](http://blog.scoutapp.com/articles/2018/01/15/tutorial-tracing-python-flask-requests-with-opentracing) walks through the setup.
