import zmq

context = zmq.Context()
socket = context.socket(zmq.SUB)
socket.connect("tcp://127.0.0.1:5556")
socket.setsockopt(zmq.SUBSCRIBE, b'camera')

while True:
    topic = socket.recv_string()
    data = socket.recv()

    print(data)
