using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkerforge;

namespace IoT
{
    internal class Component
    {
        private IPConnection _ipConnection;
        private BrickletDualButton _dualButtonBricklet;
        private BrickletLCD20x4 _lcdBricklet;
        private BrickletTemperature _temperatureBricklet;
        private BrickletHumidity _humidityBricklet;
        private BrickletLinearPoti _linearPoti;
        private BrickletRotaryPoti _rotaryPoti;
        private BrickletRGBLEDButton _rgbButton;
        private BrickletSegmentDisplay4x7 _segmentDisplay;
        private BrickletMotionDetectorV2 _motionDetector;

        //TODO create configurable file for this.
        private static string HOST = "localhost";
        private static int PORT = 4223;
        private const string TemperatureUID = "ENS";
        private const string DualButtonUID = "vQJ";
        private const string DisplayUID = "BKh";
        private const string HumidityUID = "Dhd";
        private const string LinearPotiUID = "zZL";
        private const string RGBButtonUID = "Dmk";
        private const string RotaryPotiUID = "y6z";
        private const string SegmentUID = "wKt";
        private const string motionDetectorUID = "EkJ";

        //setup variables
        private byte _red = 150;
        private byte _green = 150;
        private byte _blue = 0;
        private int counter = 0;

        //setup consts
        private const byte RED = 150;
        private const byte GREEN = 150;
        private const byte BLUE = 0;

        private static byte[] DIGITS = {0x3f,0x06,0x5b,0x4f,
	                                0x66,0x6d,0x7d,0x07,
	                                0x7f,0x6f,0x77,0x7c,
	                                0x39,0x5e,0x79,0x71};
        

        public Component()
        {
            // Create connection object
            _ipConnection = new IPConnection();

            // Create device objects
            _dualButtonBricklet = new BrickletDualButton(DualButtonUID, _ipConnection);
            _lcdBricklet = new BrickletLCD20x4(DisplayUID, _ipConnection);
            _temperatureBricklet = new BrickletTemperature(TemperatureUID, _ipConnection);
            _humidityBricklet = new BrickletHumidity(HumidityUID, _ipConnection);
            _linearPoti = new BrickletLinearPoti(LinearPotiUID, _ipConnection);
            _rgbButton = new BrickletRGBLEDButton(RGBButtonUID, _ipConnection);
            _rotaryPoti = new BrickletRotaryPoti(RotaryPotiUID, _ipConnection);
            _segmentDisplay = new BrickletSegmentDisplay4x7(SegmentUID, _ipConnection);
            _motionDetector = new BrickletMotionDetectorV2(motionDetectorUID, _ipConnection);

            //register listeners
            _dualButtonBricklet.StateChangedCallback += DualButtonStateChanged;

            //register callback
            _linearPoti.PositionCallback += PositionCB;
            _rotaryPoti.PositionCallback += PositionRCB;
            _motionDetector.MotionDetectedCallback += MotionDetectedCB;
            _motionDetector.DetectionCycleEndedCallback += DetectionCycleEndedCB;
        }

        public void setCallBackPeriod()
        {
            _linearPoti.SetPositionCallbackPeriod(50);
            _rotaryPoti.SetPositionCallbackPeriod(50);
            _rgbButton.SetColor(RED, GREEN, BLUE);
            _motionDetector.SetIndicator(255, 255, 255);
        }


        private void DualButtonStateChanged(BrickletDualButton sender, byte buttonL, byte buttonR, byte ledl, byte ledr)
        {
            _lcdBricklet.BacklightOn();
            if (buttonL == BrickletDualButton.BUTTON_STATE_PRESSED)
            {
                Console.WriteLine("Left Button: Pressed");
                _lcdBricklet.ClearDisplay();
                _lcdBricklet.WriteLine(0, 0,
                    "Teplota: " + Convert.ToString(ReadTemperature() / 100.0) + UTF16ToKS0066U(" °C"));
            }
            else if (buttonL == BrickletDualButton.BUTTON_STATE_RELEASED)
            {
                //Console.WriteLine("Left Button: Released");
            }

            if (buttonR == BrickletDualButton.BUTTON_STATE_PRESSED)
            {
                Console.WriteLine("Right Button: Pressed");
                _lcdBricklet.ClearDisplay();
                _lcdBricklet.WriteLine(0, 0,
                    "Humidity: " + Convert.ToString(ReadHumidity() / 100.0) + UTF16ToKS0066U(" %"));
            }
            else if (buttonR == BrickletDualButton.BUTTON_STATE_RELEASED)
            {
                //Console.WriteLine("Right Button: Released");
            }

            Console.WriteLine("");
        }

        public short ReadTemperature()
        {
            return _temperatureBricklet.GetTemperature();
        }

        public int ReadHumidity()
        {
            return _humidityBricklet.GetHumidity();
        }

        // Maps a normal UTF-16 encoded string to the LCD charset
        static string UTF16ToKS0066U(string utf16)
        {
            string ks0066u = "";
            char c;

            for (int i = 0; i < utf16.Length; i++)
            {
                int codePoint = Char.ConvertToUtf32(utf16, i);

                if (Char.IsSurrogate(utf16, i))
                {
                    // Skip low surrogate
                    i++;
                }

                // ASCII subset from JIS X 0201
                if (codePoint >= 0x0020 && codePoint <= 0x007e)
                {
                    // The LCD charset doesn't include '\' and '~', use similar characters instead
                    switch (codePoint)
                    {
                        case 0x005c:
                            c = (char) 0xa4;
                            break; // REVERSE SOLIDUS maps to IDEOGRAPHIC COMMA
                        case 0x007e:
                            c = (char) 0x2d;
                            break; // TILDE maps to HYPHEN-MINUS
                        default:
                            c = (char) codePoint;
                            break;
                    }
                }
                // Katakana subset from JIS X 0201
                else if (codePoint >= 0xff61 && codePoint <= 0xff9f)
                {
                    c = (char) (codePoint - 0xfec0);
                }
                // Special characters
                else
                {
                    switch (codePoint)
                    {
                        case 0x00a5:
                            c = (char) 0x5c;
                            break; // YEN SIGN
                        case 0x2192:
                            c = (char) 0x7e;
                            break; // RIGHTWARDS ARROW
                        case 0x2190:
                            c = (char) 0x7f;
                            break; // LEFTWARDS ARROW
                        case 0x00b0:
                            c = (char) 0xdf;
                            break; // DEGREE SIGN maps to KATAKANA SEMI-VOICED SOUND MARK
                        case 0x03b1:
                            c = (char) 0xe0;
                            break; // GREEK SMALL LETTER ALPHA
                        case 0x00c4:
                            c = (char) 0xe1;
                            break; // LATIN CAPITAL LETTER A WITH DIAERESIS
                        case 0x00e4:
                            c = (char) 0xe1;
                            break; // LATIN SMALL LETTER A WITH DIAERESIS
                        case 0x00df:
                            c = (char) 0xe2;
                            break; // LATIN SMALL LETTER SHARP S
                        case 0x03b5:
                            c = (char) 0xe3;
                            break; // GREEK SMALL LETTER EPSILON
                        case 0x00b5:
                            c = (char) 0xe4;
                            break; // MICRO SIGN
                        case 0x03bc:
                            c = (char) 0xe4;
                            break; // GREEK SMALL LETTER MU
                        case 0x03c2:
                            c = (char) 0xe5;
                            break; // GREEK SMALL LETTER FINAL SIGMA
                        case 0x03c1:
                            c = (char) 0xe6;
                            break; // GREEK SMALL LETTER RHO
                        case 0x221a:
                            c = (char) 0xe8;
                            break; // SQUARE ROOT
                        case 0x00b9:
                            c = (char) 0xe9;
                            break; // SUPERSCRIPT ONE maps to SUPERSCRIPT (minus) ONE
                        case 0x00a4:
                            c = (char) 0xeb;
                            break; // CURRENCY SIGN
                        case 0x00a2:
                            c = (char) 0xec;
                            break; // CENT SIGN
                        case 0x2c60:
                            c = (char) 0xed;
                            break; // LATIN CAPITAL LETTER L WITH DOUBLE BAR
                        case 0x00f1:
                            c = (char) 0xee;
                            break; // LATIN SMALL LETTER N WITH TILDE
                        case 0x00d6:
                            c = (char) 0xef;
                            break; // LATIN CAPITAL LETTER O WITH DIAERESIS
                        case 0x00f6:
                            c = (char) 0xef;
                            break; // LATIN SMALL LETTER O WITH DIAERESIS
                        case 0x03f4:
                            c = (char) 0xf2;
                            break; // GREEK CAPITAL THETA SYMBOL
                        case 0x221e:
                            c = (char) 0xf3;
                            break; // INFINITY
                        case 0x03a9:
                            c = (char) 0xf4;
                            break; // GREEK CAPITAL LETTER OMEGA
                        case 0x00dc:
                            c = (char) 0xf5;
                            break; // LATIN CAPITAL LETTER U WITH DIAERESIS
                        case 0x00fc:
                            c = (char) 0xf5;
                            break; // LATIN SMALL LETTER U WITH DIAERESIS
                        case 0x03a3:
                            c = (char) 0xf6;
                            break; // GREEK CAPITAL LETTER SIGMA
                        case 0x03c0:
                            c = (char) 0xf7;
                            break; // GREEK SMALL LETTER PI
                        case 0x0304:
                            c = (char) 0xf8;
                            break; // COMBINING MACRON
                        case 0x00f7:
                            c = (char) 0xfd;
                            break; // DIVISION SIGN

                        default:
                        case 0x25a0:
                            c = (char) 0xff;
                            break; // BLACK SQUARE
                    }
                }

                // Special handling for 'x' followed by COMBINING MACRON
                if (c == (char) 0xf8)
                {
                    if (!ks0066u.EndsWith("x"))
                    {
                        c = (char) 0xff; // BLACK SQUARE
                    }

                    if (ks0066u.Length > 0)
                    {
                        ks0066u = ks0066u.Remove(ks0066u.Length - 1, 1);
                    }
                }

                ks0066u += c;
            }

            return ks0066u;
        }

        public void Disconnect()
        {
            _ipConnection.Disconnect();
        }

        public void Connect()
        {
            _ipConnection.Connect(HOST, PORT);
        }

        //LinearPoti

        public int getPosition()
        {
            return _linearPoti.GetPosition();
        }

        public void PositionCB(BrickletLinearPoti sender, int position)
	    {
		    Console.WriteLine("Position: " + position);
            _red = (byte)(RED + ((position - 75)*2));
            _rgbButton.SetColor(_red, _green, _blue);
            WriteDigits(_red+_green+_blue);
            
	    }

        //rotaryPoti

        public int getRPosition()
        {
            return _rotaryPoti.GetPosition();
        }

        public void PositionRCB(BrickletRotaryPoti sender, short position)
	    {
		    Console.WriteLine("Position: " + position);
             _green = (byte)((GREEN + position)/1.5);
            _rgbButton.SetColor(_red, _green, _blue);
            WriteDigits(_red+_green+_blue);
	    }

        //segmentDisplay
        public void WriteDigits(int value)
        {
            var numbers = GetArray(value);
            byte[] segments= new byte[4];
            for (int i = 0; i < numbers.Length; i++)
			{
                segments[i] = DIGITS[numbers[i]];
			}
		    _segmentDisplay.SetSegments(segments, 7, false);
        }

        byte[] GetArray(int num)
        {
            List<byte> list = new List<byte>();
            if (num == 0)
	        {
                list.Add((byte) 0);
	        }
            while(num > 0)
            {
                list.Add((byte)(num % 10));
                num = num / 10;
            }
            list.Reverse();
            return list.ToArray();
        }       

        //motion detector
        public void MotionDetectedCB(BrickletMotionDetectorV2 sender)
	    {
            counter++;
		    Console.WriteLine($"Motion Detected => count: {counter} \n(next detection possible in ~2 seconds)");
	    }

        public void DetectionCycleEndedCB(BrickletMotionDetectorV2 sender)
	    {
		    Console.WriteLine("Detection Cycle Ended");
	    }
    }
}