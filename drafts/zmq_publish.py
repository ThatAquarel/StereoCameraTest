import zmq

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind("tcp://*:5556")

topic = 'camera_frame'
while True:
    socket.send_string(topic, zmq.SNDMORE)
    socket.send_string("test")
