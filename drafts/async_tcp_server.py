import asyncio
import struct

import cv2
import numpy as np

frames: list[np.ndarray] = []


class EchoServerProtocol(asyncio.Protocol):
    def __init__(self):
        self.transport = None

        self.pixels = None
        self.offset = 0
        self.resolution = np.array([0, 0], dtype=int)

        self.dt = np.dtype(np.ubyte)
        self.dt = self.dt.newbyteorder(">")

    def connection_made(self, transport):
        self.transport = transport

    def data_received(self, data):
        if len(data) == 12:
            header = struct.unpack(">ccicci", data[0:12])

            if header[0:2] == (b'w', b'=') and header[3:5] == (b'h', b'='):
                self.resolution[0:2] = header[2], header[5]
                self.pixels = np.empty(header[2] * header[5] * 3, dtype=self.dt)
                self.offset = 0
        else:
            frame_view = np.frombuffer(data, dtype=self.dt)
            self.pixels[self.offset: self.offset + frame_view.shape[0]] = frame_view
            self.offset += frame_view.shape[0]

            if self.offset >= self.pixels.shape[0]:
                global frames
                frame = np.reshape(self.pixels, (*self.resolution, 3), "F")
                frame = np.rot90(frame)
                frames.append(frame)

                frames_length = len(frames)
                if frames_length > 2:
                    frames = frames[frames_length - 2:]


async def main():
    loop = asyncio.get_running_loop()

    server = await loop.create_server(
        lambda: EchoServerProtocol(),
        "127.0.0.1", 5005)

    async with server:
        await asyncio.gather(
            server.serve_forever(),
            frame_reader()
        )


async def frame_reader():
    global frames

    while len(frames) < 1:
        await asyncio.sleep(0.1)

    while True:
        await asyncio.sleep(0.1)

        cv2.imshow('frame', frames[0][:, :, (2, 1, 0)])

        if cv2.waitKey(1) == ord('q'):
            break

    cv2.destroyAllWindows()


if __name__ == '__main__':
    asyncio.run(main())
