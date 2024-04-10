using System;
using System.Timers;

namespace chip_emulator
{
    internal readonly struct Memory
    {
        internal readonly byte[] RAM = new byte[4096];

        private readonly byte[] V_registers = new byte[16]; //16 registers to store data/variables

        private readonly byte[] I_register = new byte[2]; //Register usually used to store memory addresses
        private readonly byte[] PC_register = new byte[2]; //Register storing currently executing memory address aka the program counter

        private readonly byte[,] Stack = new byte[16,2]; //Stack storing addresses of max 16 nesting subroutines
        private readonly byte[] SP_register = new byte[1]; //Register pointing to topmost level of stack

        private readonly byte[] DT_register = new byte[1]; //Delay timer register, decremented at 60Hz as soon as it's non-zero
        private readonly byte[] ST_register = new byte[1]; //Sound timer register, same as DT

        private const ushort FONT_ADDRESS = 0x50;
        private const ushort FONT_ADDRESS_END = 0xA0;

        public Memory()
        {
            ushort startingAddress = 0x200; //0x000 to 0x1FF contained the original interpreter, programs start after that
            SetPCAddress(startingAddress);

            //Add hexadecimal font to memory
            byte[] font  = new byte[]{
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };
            for(int i = 0; i < font.Length; i++)
            {
                RAM[FONT_ADDRESS + i] = font[i];
            }
        }

        #region V-Register
        internal byte GetVRegister(int register)
        {
            if(register >= 0 && register < V_registers.Length)
            {
                return V_registers[register];
            }
            throw new ArgumentOutOfRangeException("Attempted to read invalid V-Register " + register + ".");
        }

        internal void SetVRegister(int register, byte value)
        {
            if(register >= 0 && register < V_registers.Length)
            {
                V_registers[register] = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Attempted to write " + value + " to invalid V-Register " + register + ".");
            }
        }
        #endregion V-Register

        #region I-Register
        internal ushort GetIAddress()
        {
            return BitConverter.ToUInt16(I_register);
        }

        internal void SetIAddress(ushort address)
        {
            byte[] data = BitConverter.GetBytes(address);
            I_register[0] = data[0];
            I_register[1] = data[1];
        }
        #endregion I-Register

        #region PC-Register
        internal ushort GetPCAddress()
        {
            return BitConverter.ToUInt16(PC_register);
        }

        internal void SetPCAddress(ushort address)
        {
            byte[] data = BitConverter.GetBytes(address);
            PC_register[0] = data[0];
            PC_register[1] = data[1];
        }

        internal void IncrementPC()
        {
            ushort address = GetPCAddress();
            address += 2;
            SetPCAddress(address);
        }
        #endregion PC-Register

        #region Stack
        internal void EnterSubroutine(ushort address)
        {
            if(SP_register[0] < Stack.Length)
            {
                byte first = PC_register[0];
                byte second = PC_register[1];
                byte pointer = SP_register[0];

                Stack[pointer, 0] = first;
                Stack[pointer, 1] = second;
                SP_register[0] = (byte)(pointer + 1);

                SetPCAddress(address);
            }
            else
            {
                throw new StackOverflowException("Tried to enter more than 16 subroutines with address " + address + ".");
            }
        }

        internal void ReturnSubroutine()
        {
            if(SP_register[0] != 0)
            {
                int pointer = SP_register[0] - 1;
                SP_register[0] = (byte)pointer;

                byte[] stack = new byte[] { Stack[pointer, 0], Stack[pointer, 1] };
                ushort address = BitConverter.ToUInt16(stack);

                SetPCAddress(address);
            }
        }
        #endregion Stack

        #region Timers
        internal byte GetDTRegister()
        {
            return DT_register[0];
        }

        internal void SetDelayTimer(byte delay)
        {
            DT_register[0] = delay;

            Timer timer = new(1000d / 60d);
            timer.Elapsed += DoDelayTimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void DoDelayTimerTick(object source, ElapsedEventArgs e)
        {
            byte timerValue = DT_register[0];
            timerValue--;

            if(timerValue <= 0)
            {
                DT_register[0] = 0;
                ((Timer)source).Enabled = false;
                ((Timer)source).Close();
            }
            else
            {
                DT_register[0] = timerValue;
            }
        }

        internal void SetSoundTimer(byte delay)
        {
            ST_register[0] = delay;

            Timer timer = new(1000d / 60d);
            timer.Elapsed += DoSoundTimerTick;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void DoSoundTimerTick(object source, ElapsedEventArgs e)
        {
            byte timerValue = ST_register[0];
            timerValue--;
            
            if (timerValue <= 0)
            {
                ST_register[0] = 0;
                ((Timer)source).Enabled = false;
                ((Timer)source).Close();
            }
            else
            {
                ST_register[0] = timerValue;
            }
        }
        #endregion Timers

        internal void SetIToFont(byte font)
        {
            ushort address = (ushort)(FONT_ADDRESS + (font * 5));
            SetIAddress(address);
        }

        internal bool IsValidAddress(int address)
        {
            //Programs get stored into memory starting at address 0x200
            //Final 352 bytes of memory are reserved for "variables and display refresh"
            return (address >= 0x200 && address < RAM.Length - 352) || (address >= FONT_ADDRESS && address < FONT_ADDRESS_END);
        }
    }
}
