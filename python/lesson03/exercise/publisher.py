from flask import Flask
from flask import request

app = Flask(__name__)
 
@app.route("/publish")
def format():
    hello_str = request.args.get('helloStr')
    print(hello_str)
    return 'published'
 
if __name__ == "__main__":
    app.run(port=8082)
