using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace chip_emulator
{
    internal class Interpreter
    {
        internal static readonly int CLOCK_RATE = 500; //Adjust as wanted to change execution speed

        private readonly Memory _memory;
        private readonly Random _random;

        private bool _clockTicked = false;
        private readonly bool _executionFinished = false;

        internal Interpreter(string programPath)
        {
            _memory = new Memory();
            _random = new Random();

            if(File.Exists(programPath))
            {
                byte[] program = File.ReadAllBytes(programPath);
                
                if(_memory.IsValidAddress(0x200 + (program.Length - 1)))
                {
                    program.CopyTo(_memory.RAM, 0x200);
                }
                else
                {
                    throw new InsufficientMemoryException("File " + programPath + " too big to load into memory.");
                }
            }
            else
            {
                throw new ArgumentException("Invalid file path passed to Interpreter " + programPath + ".");
            }
        }

        internal void Run()
        {
            System.Timers.Timer timer = new(1000d / CLOCK_RATE);
            timer.Elapsed += (object source, ElapsedEventArgs e) => { _clockTicked = true; };
            timer.AutoReset = true;
            timer.Enabled = true;

            do
            {
                if(_clockTicked)
                {
                    _clockTicked = false;

                    ExecuteNextLine();
                    // TODO: Implement audio engine to play sound until ST is 0
                }
            } while(!_executionFinished);
        }

        #region Utility
        private static string ToHexString(ushort number)
        {
            string hex = "";

            hex += ((number & 0xF000) >> 12).ToString("X");
            hex += ((number & 0x0F00) >> 8).ToString("X");
            hex += ((number & 0x00F0) >> 4).ToString("X");
            hex += (number & 0x000F).ToString("X");

            return hex;
        }

        private static int GetNibble(ushort data, int index)
        {
            return index switch {
                0 => (data & 0xF000) >> 12,
                1 => (data & 0x0F00) >> 8,
                2 => (data & 0x00F0) >> 4,
                3 => (data & 0x000F),
                4 => (data & 0x00FF),
                5 => (data & 0x0FFF),
                _ => 0,
            };
        }

        private static bool IsKeyDown(byte key)
        {
            KeyboardState state = Keyboard.GetState();
            return key switch {
                0x1 => state.IsKeyDown(Keys.D1),
                0x2 => state.IsKeyDown(Keys.D2),
                0x3 => state.IsKeyDown(Keys.D3),
                0xC => state.IsKeyDown(Keys.D4),
                //
                0x4 => state.IsKeyDown(Keys.Q),
                0x5 => state.IsKeyDown(Keys.W),
                0x6 => state.IsKeyDown(Keys.E),
                0xD => state.IsKeyDown(Keys.R),
                //
                0x7 => state.IsKeyDown(Keys.A),
                0x8 => state.IsKeyDown(Keys.S),
                0x9 => state.IsKeyDown(Keys.D),
                0xE => state.IsKeyDown(Keys.F),
                //
                0xA => state.IsKeyDown(Keys.Z),
                0x0 => state.IsKeyDown(Keys.X),
                0xB => state.IsKeyDown(Keys.C),
                0xF => state.IsKeyDown(Keys.V),
                _ => false,
            };
        }

        private static int GetPressedKey()
        {
            KeyboardState state = Keyboard.GetState();
            if(state.IsKeyDown(Keys.D1)) return 0x1;
            if(state.IsKeyDown(Keys.D2)) return 0x2;
            if(state.IsKeyDown(Keys.D3)) return 0x3;
            if(state.IsKeyDown(Keys.D4)) return 0xC;
            //
            if(state.IsKeyDown(Keys.Q)) return 0x4;
            if(state.IsKeyDown(Keys.W)) return 0x5;
            if(state.IsKeyDown(Keys.E)) return 0x6;
            if(state.IsKeyDown(Keys.R)) return 0xD;
            //
            if(state.IsKeyDown(Keys.A)) return 0x7;
            if(state.IsKeyDown(Keys.S)) return 0x8;
            if(state.IsKeyDown(Keys.D)) return 0x9;
            if(state.IsKeyDown(Keys.F)) return 0xE;
            //
            if(state.IsKeyDown(Keys.Z)) return 0xA;
            if(state.IsKeyDown(Keys.X)) return 0x0;
            if(state.IsKeyDown(Keys.C)) return 0xB;
            if(state.IsKeyDown(Keys.V)) return 0xF;
            return -1;
        }
        #endregion Utility

        private void ExecuteNextLine()
        {
            ushort PC_Address = _memory.GetPCAddress();
            ushort instruction = (ushort)((_memory.RAM[PC_Address] << 8) + _memory.RAM[PC_Address + 1]);
            _memory.IncrementPC();

            switch(GetNibble(instruction, 0))
            {
                case 0x0:
                    if(instruction == 0x00E0)
                    {
                        Instruction_00E0();
                    }
                    else if(instruction == 0x00EE)
                    {
                        Instruction_00EE();
                    }
                    else
                    {
                        throw new NotImplementedException(
                            string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                            );
                    }
                    break;
                case 0x1:
                    Instruction_1NNN(instruction);
                    break;
                case 0x2:
                    Instruction_2NNN(instruction);
                    break;
                case 0x3:
                    Instruction_3XKK(instruction);
                    break;
                case 0x4:
                    Instruction_4XKK(instruction);
                    break;
                case 0x5:
                    if(GetNibble(instruction, 3) == 0x0)
                    {
                        Instruction_5XY0(instruction);
                    }
                    else
                    {
                        throw new NotImplementedException(
                            string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                            );
                    }
                    break;
                case 0x6:
                    Instruction_6XKK(instruction);
                    break;
                case 0x7:
                    Instruction_7XKK(instruction);
                    break;
                case 0x8:
                    switch(GetNibble(instruction, 3))
                    {
                        case 0x0:
                            Instruction_8XY0(instruction);
                            break;
                        case 0x1:
                            Instruction_8XY1(instruction);
                            break;
                        case 0x2:
                            Instruction_8XY2(instruction);
                            break;
                        case 0x3:
                            Instruction_8XY3(instruction);
                            break;
                        case 0x4:
                            Instruction_8XY4(instruction);
                            break;
                        case 0x5:
                            Instruction_8XY5(instruction);
                            break;
                        case 0x6:
                            Instruction_8XY6(instruction);
                            break;
                        case 0x7:
                            Instruction_8XY7(instruction);
                            break;
                        case 0xE:
                            Instruction_8XYE(instruction);
                            break;
                        default:
                            throw new NotImplementedException(
                                string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                                );
                    }
                    break;
                case 0x9:
                    if(GetNibble(instruction, 3) == 0x0)
                    {
                        Instruction_9XY0(instruction);
                    }
                    else
                    {
                        throw new NotImplementedException(
                            string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                            );
                    }
                    break;
                case 0xA:
                    Instruction_ANNN(instruction);
                    break;
                case 0xB:
                    Instruction_BNNN(instruction);
                    break;
                case 0xC:
                    Instruction_CXKK(instruction);
                    break;
                case 0xD:
                    Instruction_DXYN(instruction);
                    break;
                case 0xE:
                    switch(GetNibble(instruction, 4))
                    {
                        case 0x9E:
                            Instruction_EX9E(instruction);
                            break;
                        case 0xA1:
                            Instruction_EXA1(instruction);
                            break;
                        default:
                            throw new NotImplementedException(
                                string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                                );

                    }
                    break;
                case 0xF:
                    switch(GetNibble(instruction, 4))
                    {
                        case 0x07:
                            Instruction_FX07(instruction);
                            break;
                        case 0x0A:
                            Instruction_FX0A(instruction);
                            break;
                        case 0x15:
                            Instruction_FX15(instruction);
                            break;
                        case 0x18:
                            Instruction_FX18(instruction);
                            break;
                        case 0x1E:
                            Instruction_FX1E(instruction);
                            break;
                        case 0x29:
                            Instruction_FX29(instruction);
                            break;
                        case 0x33:
                            Instruction_FX33(instruction);
                            break;
                        case 0x55:
                            Instruction_FX55(instruction);
                            break;
                        case 0x65:
                            Instruction_FX65(instruction);
                            break;
                        default:
                            throw new NotImplementedException(
                                string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                                );
                    }
                    break;
                default:
                    throw new NotImplementedException(
                        string.Format("Unknown Instruction {0} at address {1}", ToHexString(instruction), ToHexString(PC_Address))
                        );
            }
        }

        #region Instructions
        // Instructions referenced from http://devernay.free.fr/hacks/chip8/C8TECH10.HTM and https://tobiasvl.github.io/blog/write-a-chip-8-emulator/

        private void Instruction_00E0()
        {
            Display.Clear();
        }

        private void Instruction_00EE()
        {
            _memory.ReturnSubroutine();
        }

        private void Instruction_1NNN(ushort instruction)
        {
            int address = GetNibble(instruction, 5);
            _memory.SetPCAddress((ushort)address);
        }

        private void Instruction_2NNN(ushort instruction)
        {
            int address = GetNibble(instruction, 5);
            _memory.EnterSubroutine((ushort)address);
        }

        private void Instruction_3XKK(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            int currValue = _memory.GetVRegister(register);
            int compareValue = GetNibble(instruction, 4);

            if(currValue == compareValue)
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_4XKK(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            int currValue = _memory.GetVRegister(register);
            int compareValue = GetNibble(instruction, 4);

            if(currValue != compareValue)
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_5XY0(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            int valueX = _memory.GetVRegister(registerX);
            int valueY = _memory.GetVRegister(registerY);

            if(valueX == valueY)
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_6XKK(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            int value = GetNibble(instruction, 4);

            _memory.SetVRegister(register, (byte)value);
        }

        private void Instruction_7XKK(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte currValue = _memory.GetVRegister(register);
            int addValue = GetNibble(instruction, 4);
            int sum = currValue + addValue;

            _memory.SetVRegister(register, (byte)sum);
        }

        private void Instruction_8XY0(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueY = _memory.GetVRegister(registerY);

            _memory.SetVRegister(registerX, valueY);
        }

        private void Instruction_8XY1(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            int newValue = valueX | valueY;
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XY2(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            int newValue = valueX & valueY;
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XY3(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            int newValue = valueX ^ valueY;
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XY4(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            int valueX = _memory.GetVRegister(registerX);
            int valueY = _memory.GetVRegister(registerY);

            int newValue = valueX + valueY;
            if(newValue > 255)
            {
                _memory.SetVRegister(0xF, 1);
            }
            else
            {
                _memory.SetVRegister(0xF, 0);
            }
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XY5(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            if(valueX > valueY)
            {
                _memory.SetVRegister(0xF, 1);
            }
            else
            {
                _memory.SetVRegister(0xF, 0);
            }
            int newValue = valueX - valueY;
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XY6(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueY = _memory.GetVRegister(registerY);

            int newValue = valueY >> 1;
            _memory.SetVRegister(registerX, (byte)newValue);

            int bitValue = valueY & 0x01;
            _memory.SetVRegister(0xF, (byte)bitValue);
        }

        private void Instruction_8XY7(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            if(valueY > valueX)
            {
                _memory.SetVRegister(0xF, 1);
            }
            else
            {
                _memory.SetVRegister(0xF, 0);
            }
            int newValue = valueY - valueX;
            _memory.SetVRegister(registerX, (byte)newValue);
        }

        private void Instruction_8XYE(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueY = _memory.GetVRegister(registerY);

            int newValue = valueY << 1;
            _memory.SetVRegister(registerX, (byte)newValue);

            int bitValue = valueY & 0x80;
            _memory.SetVRegister(0xF, (byte)bitValue);
        }

        private void Instruction_9XY0(ushort instruction)
        {
            int registerX = GetNibble(instruction, 1);
            int registerY = GetNibble(instruction, 2);
            byte valueX = _memory.GetVRegister(registerX);
            byte valueY = _memory.GetVRegister(registerY);

            if(valueX != valueY)
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_ANNN(ushort instruction)
        {
            int address = GetNibble(instruction, 5);
            _memory.SetIAddress((ushort)address);
        }

        private void Instruction_BNNN(ushort instruction)
        {
            int address = GetNibble(instruction, 5);
            int addValue = _memory.GetVRegister(0);

            int newAddress = address + addValue;
            _memory.SetPCAddress((ushort)newAddress);
        }

        private void Instruction_CXKK(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            int randomNumber = _random.Next(256);
            int andValue = GetNibble(instruction, 4);

            int newValue = randomNumber & andValue;
            _memory.SetVRegister(register, (byte)newValue);
        }

        private void Instruction_DXYN(ushort instruction)
        {
            int xRegister = GetNibble(instruction, 1);
            int x = _memory.GetVRegister(xRegister);

            int yRegister = GetNibble(instruction, 2);
            int y = _memory.GetVRegister(yRegister);

            int dataCount = GetNibble(instruction, 3);
            int startingAddress = _memory.GetIAddress();
            List<byte> spriteData = new();
            for(int i = startingAddress; i < startingAddress + dataCount; i++)
            {
                if(_memory.IsValidAddress(i))
                {
                    spriteData.Add(_memory.RAM[i]);
                }
                else
                {
                    string message = string.Format(
                        "Attempted to access protected Memory address {0} with instruction {1} at PC_address {2}",
                        i,
                        ToHexString(instruction),
                        ToHexString(_memory.GetPCAddress())
                        );
                    throw new AccessViolationException(message);
                }
            }

            bool collision = Display.DisplaySprite(spriteData, x, y);
            if(collision)
            {
                _memory.SetVRegister(0xF, 0x01);
            }
            else
            {
                _memory.SetVRegister(0xF, 0x00);
            }
        }

        private void Instruction_EX9E(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte key = _memory.GetVRegister(register);

            if(IsKeyDown(key))
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_EXA1(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte key = _memory.GetVRegister(register);

            if(!IsKeyDown(key))
            {
                _memory.IncrementPC();
            }
        }

        private void Instruction_FX07(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte time = _memory.GetDTRegister();
            _memory.SetVRegister(register, time);
        }

        private void Instruction_FX0A(ushort instruction)
        {
            int register = GetNibble(instruction, 1);

            bool isKeyPressed = false;
            int pressedKey;
            do
            {
                pressedKey = GetPressedKey();
                if(pressedKey != -1)
                {
                    isKeyPressed = true;
                }
            } while(!isKeyPressed);
            _memory.SetVRegister(register, (byte)pressedKey);
        }

        private void Instruction_FX15(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte time = _memory.GetVRegister(register);
            _memory.SetDelayTimer(time);
        }

        private void Instruction_FX18(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte time = _memory.GetVRegister(register);
            _memory.SetSoundTimer(time);
        }

        private void Instruction_FX1E(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte valueX = _memory.GetVRegister(register);
            ushort valueI = _memory.GetIAddress();

            int newValue = valueI + valueX;
            _memory.SetIAddress((ushort)newValue);
        }

        private void Instruction_FX29(ushort instruction)
        {
            int register = GetNibble(instruction, 1);
            byte digit = _memory.GetVRegister(register);
            if(digit <= 0xF)
            {
                _memory.SetIToFont(digit);
            }
            else
            {
                string message = string.Format(
                    "Attempted to access invalid digit {0} with instruction {1} at PC_address {2}",
                    digit,
                    ToHexString(instruction),
                    ToHexString(_memory.GetPCAddress())
                    );
                throw new ArgumentOutOfRangeException(message);
            }
        }

        private void Instruction_FX33(ushort instruction)
        {
            int startingAddress = _memory.GetIAddress();
            if(_memory.IsValidAddress(startingAddress) && _memory.IsValidAddress(startingAddress + 2))
            {
                int register = GetNibble(instruction, 1);
                int value = _memory.GetVRegister(register);

                int hundreds = value / 100;
                int tens = (value / 10) % 10;
                int ones = value % 10;

                _memory.RAM[startingAddress] = (byte)hundreds;
                _memory.RAM[startingAddress + 1] = (byte)tens;
                _memory.RAM[startingAddress + 2] = (byte)ones;
            }
            else
            {
                string message = string.Format(
                        "Attempted to access protected Memory range {0} to {1} with instruction {2} at PC_address {3}",
                        startingAddress,
                        startingAddress + 2,
                        ToHexString(instruction),
                        ToHexString(_memory.GetPCAddress())
                        );
                throw new AccessViolationException(message);
            }
        }

        private void Instruction_FX55(ushort instruction)
        {
            int startingAddress = _memory.GetIAddress();
            int endRegister = GetNibble(instruction, 1);
            if(_memory.IsValidAddress(startingAddress) && _memory.IsValidAddress(startingAddress + endRegister))
            {
                for(int register = 0; register <= endRegister; register++)
                {
                    byte value = _memory.GetVRegister(register);
                    _memory.RAM[startingAddress + register] = value;
                }
            }
            else
            {
                string message = string.Format(
                        "Attempted to access protected Memory range {0} to {1} with instruction {2} at PC_address {3}",
                        startingAddress,
                        startingAddress + endRegister,
                        ToHexString(instruction),
                        ToHexString(_memory.GetPCAddress())
                        );
                throw new AccessViolationException(message);
            }
        }

        private void Instruction_FX65(ushort instruction)
        {
            int startingAddress = _memory.GetIAddress();
            int endRegister = GetNibble(instruction, 1);
            if(_memory.IsValidAddress(startingAddress) && _memory.IsValidAddress(startingAddress + endRegister))
            {
                for(int register = 0; register <= endRegister; register++)
                {
                    byte value = _memory.RAM[startingAddress + register];
                    _memory.SetVRegister(register, value);
                }
            }
            else
            {
                string message = string.Format(
                        "Attempted to access protected Memory range {0} to {1} with instruction {2} at PC_address {3}",
                        startingAddress,
                        startingAddress + endRegister,
                        ToHexString(instruction),
                        ToHexString(_memory.GetPCAddress())
                        );
                throw new AccessViolationException(message);
            }
        }
        #endregion Instructions
    }
}
