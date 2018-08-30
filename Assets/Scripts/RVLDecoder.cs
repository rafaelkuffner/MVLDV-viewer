using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class RVLDecoder
{

    int word,buffer, pBuffer;
	int nibblesWritten;

    FileStream _inFile;
    int _readPosition;
    byte[] _input;
    byte[] _sizeBuffer;
    List<long> _FrameIndex;
    public bool prepared;
    public RVLDecoder(string depthFile,int width, int height)
    {
        buffer = pBuffer =  nibblesWritten = 0;
		word = 0;

        _inFile = new FileStream(depthFile,FileMode.Open);
        if (_input == null)
        {
            _input = new byte[width*height];
            _sizeBuffer = new byte[4];
        }
        _FrameIndex = new List<long>();
        prepared = false;
    }

    public void Prepare()
    {
        ResetDecoder();
        int bytesRead = _inFile.Read(_sizeBuffer, 0, 4);
        long index = 0;
        long i = 0;
        while (bytesRead != 0)
        {
            _FrameIndex.Add(index);

            int size = (_sizeBuffer[0] << 24) | (_sizeBuffer[1] << 16) | (_sizeBuffer[2] << 8) | (_sizeBuffer[3]);
            
            index += 4 + size;
            _inFile.Position += size; 
            i++;
            bytesRead = _inFile.Read(_sizeBuffer, 0, 4);
            //if(i % 30 == 0) yield return null;
        }
        ResetDecoder();
        prepared = true;
        Debug.Log("Depth prepared!");
    }

    public void ResetDecoder()
    {
        _inFile.Position = 0;
        buffer = pBuffer = nibblesWritten = 0;
        word = 0;


    }

    public void skipToFrame(long frame)
    {
        _inFile.Position = _FrameIndex[(int)frame];
         
    }

    int DecodeVLE(byte[] input)
    {
		uint nibble;

		int value = 0, bits = 29;
        do
        {
            if (nibblesWritten == 0)
            {
				word = (int) (input[pBuffer] | (input[pBuffer + 1] << 8)) | ((input[pBuffer + 2] << 0x10) | (input[pBuffer + 3] << 0x18));  // load word
				pBuffer += 4;
                nibblesWritten = 8;
            }
            uint mask = 0xf0000000;
            nibble = (uint) word & mask;
			uint nibblebits =  (nibble<<1) >> bits;
			value = value | (int)nibblebits;
            word <<= 4;
            nibblesWritten--;
            bits -= 3;
		} while ((nibble & 0x80000000) != 0);
        return value;
    }


    public bool DecompressRVL(byte[] output, int numPixels)
    {
       int bytesRead = _inFile.Read(_sizeBuffer, 0, 4);
        if (bytesRead == 0)
        {
            return false;
        }
        int size = (_sizeBuffer[0] << 24) | (_sizeBuffer[1] << 16) | (_sizeBuffer[2] << 8) | (_sizeBuffer[3]);

        _inFile.Read(_input, 0, size);


        buffer = pBuffer = 0;
        nibblesWritten = 0;
        int current, previous = 0;
        int numPixelsToDecode = numPixels;
        int k = 0;
        while (numPixelsToDecode > 0)
        {
            int zeros = DecodeVLE(_input); // number of zeros
            numPixelsToDecode -= zeros;
			for (; zeros != 0; zeros--) {
                if(k+4 > output.Length)
                {
                    Debug.Log("Frame error decompress!");
                    return false;
                }
				output [k++] = 0;
				output [k++] = 0;
				output [k++] = 0;
				output [k++] = 0;
			}
            if (numPixelsToDecode == 0) return true;
            int nonzeros = DecodeVLE(_input); // number of nonzeros
            numPixelsToDecode -= nonzeros;
            for (; nonzeros != 0; nonzeros--)
            {
                int positive = DecodeVLE(_input); // nonzero value
                int delta = (positive >> 1) ^ -(positive & 1);
                current = (previous + delta);
                if (k + 4 > output.Length)
                {
                    Debug.Log("Frame error decompress!");
                    return false;
                }
                output[k++] = (byte)current;
                output[k++] = (byte)(current >> 8);
                output[k++] = (byte)(current >> 0x10);
                output[k++] = (byte)(current >> 0x18); ;
                previous = current;
            }
        }
        return true;

    }

}