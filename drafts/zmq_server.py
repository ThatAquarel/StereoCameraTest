import zmq
import numpy as np

import cv2

context = zmq.Context()
socket = context.socket(zmq.SUB)
socket.connect("tcp://127.0.0.1:5556")
socket.setsockopt(zmq.SUBSCRIBE, b'camera')

dt = np.dtype(np.ubyte).newbyteorder(">")

while True:
    topic = socket.recv_string()
    data = socket.recv()

    frame = np.frombuffer(data, dtype=dt)
    frame = np.reshape(frame, (1080, 1920, 3))
    frame = np.flip(frame, axis=0)

    # frame = cv2.resize(frame[:, :, (2, 1, 0)], (1920 // 2, 1080 // 2))
    frame = cv2.resize(frame[:, :, (2, 1, 0)], (1920 // 2, 1080 // 2))
    cv2.imshow('frame', frame)

    if cv2.waitKey(1) == ord('q'):
        break

cv2.destroyAllWindows()
socket.close()
