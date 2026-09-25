using System;
using System.Globalization;
using System.Linq;
using Trolley_Control;

namespace Temperature_Monitor
{
    class IsotechMilliK : IDisposable
    {
        private readonly object thisLock = new object();
        private readonly Client milliKClientSocket;


        private short[] channelMap;
        private PRT[] prtArray = new PRT[32];

        private double correctionA1_1;
        private double correctionA2_1;
        private double correctionA3_1;
        private double correctionA4_1;

        private double correctionA1_2;
        private double correctionA2_2;
        private double correctionA3_2;
        private double correctionA4_2;

        private double correctionA1_3;
        private double correctionA2_3;
        private double correctionA3_3;
        private double correctionA4_3;

        private double correctionA1_4;
        private double correctionA2_4;
        private double correctionA3_4;
        private double correctionA4_4;

        private bool initialised;
        private short selectedChannel;

        private MilliKCurrent measurementCurrent =
            MilliKCurrent.Normal;

        private MilliKUnits measurementUnits =
            MilliKUnits.Celsius;

        // Maximum expected resistance. A value greater than 115 ohms
        // causes the milliK to select its 460 ohm range.
        private double maximumExpectedResistance = 200.0;

        public IsotechMilliK(ref PRT[] prts)
        {
            prtArray = prts;

            channelMap = new short[] { 10, 11, 12, 13, 14, 15, 16, 17,
                           20, 21, 22, 23, 24, 25, 26, 27,
                           30, 31, 32, 33, 34, 35, 36, 37,
                           40, 41, 42, 43, 44, 45, 46, 47 };


            milliKClientSocket = new Client
            {
                Port = 1000,
                Timeout = 5000
            };

            initialised = false;
            selectedChannel = 10;
        }

        public enum MilliKCurrent
        {
            Normal,
            Root2
        }

        public enum MilliKUnits
        {
            Celsius,
            Kelvin,
            Fahrenheit
        }

        public void setProbe(PRT probe, short channelNumber)
        {
            int index = Array.IndexOf(channelMap, channelNumber);
            prtArray[index] = probe;
        }
        public string InstrumentIdentification { get; private set; }
            = string.Empty;

        public string CalibrationStatus { get; private set; }
            = string.Empty;

        public void IP(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "An IP address or host name is required.",
                    nameof(address));
            }

            milliKClientSocket.IP = address;
        }

        public void Port(int port)
        {
            milliKClientSocket.Port = port;
        }

        public void Timeout(int timeoutMilliseconds)
        {
            milliKClientSocket.Timeout = timeoutMilliseconds;
        }

        /// <summary>
        /// Sets the milliK PRT excitation current.
        /// 0 selects normal current, nominally 1 mA.
        /// 1 selects root-2 current, nominally 1.428 mA.
        /// </summary>
        protected void setCurrent(short current)
        {
            
            switch (current)
            {
                case 0:
                    measurementCurrent = MilliKCurrent.Normal;
                    break;
                case 1:
                    measurementCurrent = MilliKCurrent.Root2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(current),
                        "The milliK accepts 0 for NORMal current " +
                        "or 1 for ROOT2 current.");
            }
            
            if (!initialised)
                return;

            if(measurementCurrent == MilliKCurrent.Normal)
            {
                SendCommand("SENSe:CURRent NORMal");
            }
            else
            {
                SendCommand("SENSe:CURRent ROOT2");
            }
            
        }

        public void SetMeasurementCurrent(
            MilliKCurrent current)
        {
            lock (thisLock)
            {
                measurementCurrent = current;

                if (initialised)
                {
                    if (current == MilliKCurrent.Normal) SendCommand("SENSe:CURRent NORMal");
                    else if (current == MilliKCurrent.Root2) SendCommand("SENSe:CURRent ROOT2");
                    else throw new ArgumentOutOfRangeException(nameof(current),"The milliK accepts Normal or Root2 current.");

                }
            }
        }

        protected void setRemoteMode()
        {
            SendCommand("MILLik:REMote");
        }

        public void setCurrentChannel(short channelNumber)
        {
            ValidateChannel(channelNumber);

            lock (thisLock)
            {
                EnsureInitialised();

                SendCommand(
                    $"SENSe:CHANnel {channelNumber}");

                selectedChannel = channelNumber;
            }
        }

        public short getCurrentChannel()
        {
            return selectedChannel;
        }

        /// <summary>
        /// Connects to the milliK, verifies its identity and calibration,
        /// places it in remote mode, and establishes four-wire resistance
        /// measurement settings.
        /// </summary>
        public void Init()
        {
            if (initialised)
                return;

            if (string.IsNullOrWhiteSpace(milliKClientSocket.IP))
            {
                throw new InvalidOperationException(
                    "Set the milliK IP address before initialising.");
            }

            if (!milliKClientSocket.Connect())
            {
                throw new InvalidOperationException(
                    $"Could not connect to the milliK at " +
                    $"{milliKClientSocket.IP}:" +
                    $"{milliKClientSocket.Port}.");
            }

            try
            {
                InstrumentIdentification = Query("*IDN?");

                if (!InstrumentIdentification.Contains(
                        "milliK"))
                {
                    throw new InvalidOperationException(
                        "The connected device did not identify itself " +
                        $"as a milliK. Response: " +
                        $"{InstrumentIdentification}");
                }

                /*
                 * The instrument must have completed startup and the
                 * front-panel Start button must have been pressed before
                 * remote mode can be entered.
                 */
                setRemoteMode();

                CalibrationStatus =
                    Query("CALibrate:VALid?");

                if (!CalibrationStatus.Contains("Calibration Okay"))
                {
                    throw new InvalidOperationException(
                        "The milliK calibration information is not " +
                        $"reported as valid. Response: " +
                        $"{CalibrationStatus}");
                }

                // Establish an explicit, known measurement state.
                SendCommand("SENSe:FUNCtion RESistance");
                SendCommand(
                    $"SENSe:RESistance:RANGe " +
                    FormatNumber(maximumExpectedResistance));
                SendCommand(
                    measurementCurrent == MilliKCurrent.Normal
                        ? "SENSe:CURRent NORMal"
                        : "SENSe:CURRent ROOT2");
                SendCommand("SENSe:RESistance:WIRes 4");

                // Select an initially valid scanner channel.
                SendCommand(
                    $"SENSe:CHANnel {selectedChannel}");

                // Confirm the applied settings.
                string function =
                    Query("SENSe:FUNCtion?");

                string wires =
                    Query("SENSe:RESistance:WIRes?");

                if (!function.Contains("RESISTANCE"))
                {
                    throw new InvalidOperationException(
                        "The milliK did not enter resistance mode. " +
                        $"Response: {function}");
                }

                if (wires.Trim() != "4")
                {
                    throw new InvalidOperationException(
                        "The milliK did not accept four-wire mode. " +
                        $"Response: {wires}");
                }

                initialised = true;
            }
            catch
            {
                milliKClientSocket.CloseConnection();
                initialised = false;
                throw;
            }
        }

        /// <summary>
        /// Reads and converts the temperature from a four-wire PRT.
        /// </summary>
        public double getTemperature(PRT probeType, short channelNumber)
        {
            return GetTemperature(probeType, channelNumber, probeHasChanged: false);
        }

        /// <summary>
        /// Returns corrected resistance for a standard resistor, or
        /// temperature in the selected units for a PRT.
        /// </summary>
        /// <param name="probeType">
        /// PRT conversion coefficients or a standard-resistor definition.
        /// </param>
        /// <param name="channelNumber">
        /// Scanner channel: 10-17, 20-27, 30-37 or 40-47.
        /// </param>
        /// <param name="probeHasChanged">
        /// Retained for compatibility. The raw resistance measurement does
        /// not require reconfiguration when the PRT object changes.
        /// </param>
        public double GetTemperature(PRT probeType, short channelNumber, bool probeHasChanged)
        {
            if (probeType == null) throw new ArgumentNullException(nameof(probeType));

            ValidateChannel(channelNumber);

            lock (thisLock)
            {
                EnsureInitialised();

                /*
                 * The one-shot command performs all of the following:
                 * select resistance function,
                 * select logical channel,
                 * select resistance range,
                 * select excitation current,
                 * select four-wire mode,
                 * initiate and return the measurement.
                 */
                string currentParameter =
                    measurementCurrent == MilliKCurrent.Normal
                        ? "NORMal"
                        : "ROOT2";

                string command =
                    $"MEASure:RESistance{channelNumber}? " +
                    $"{FormatNumber(maximumExpectedResistance)}," +
                    $"{currentParameter},4";

                string response = Query(command);

                double bridgeReading =
                    ParseInstrumentReading(response);

                selectedChannel = channelNumber;

                CorrectionCoefficients correction =
                    GetCorrectionCoefficients(channelNumber);

                double correctedResistance =ApplyCorrection(bridgeReading,correction);

                if (string.Equals(
                        probeType.PRTName,
                        "StdResistor",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return correctedResistance;
                }

                double temperatureCelsius =
                    ResistanceToTemperature(
                        correctedResistance,
                        probeType);

                return temperatureCelsius;
            }
        }

        /// <summary>
        /// Reads corrected resistance without performing temperature
        /// conversion.
        /// </summary>
        public double GetResistance(short channelNumber)
        {
            ValidateChannel(channelNumber);

            lock (thisLock)
            {
                EnsureInitialised();

                string currentParameter =
                    measurementCurrent == MilliKCurrent.Normal
                        ? "NORMal"
                        : "ROOT2";

                string response = Query(
                    $"MEASure:RESistance{channelNumber}? " +
                    $"{FormatNumber(maximumExpectedResistance)}," +
                    $"{currentParameter},4");

                double bridgeReading =
                    ParseInstrumentReading(response);

                selectedChannel = channelNumber;

                return ApplyCorrection(
                    bridgeReading,
                    GetCorrectionCoefficients(channelNumber));
            }
        }

        /// <summary>
        /// Compatibility method:
        /// 0 = Celsius, 1 = Kelvin, 2 = Fahrenheit.
        /// </summary>
        public void SetUnits(short unit)
        {
            switch (unit)
            {
                case 1:
                    measurementUnits = MilliKUnits.Celsius;
                    break;
                case 2:
                    measurementUnits = MilliKUnits.Kelvin;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), "Invalid unit specified.");
            }
        }


        public void SetUnits(MilliKUnits units)
        {
            measurementUnits = units;
        }

        public string GetInstrumentIdentification()
        {
            lock (thisLock)
            {
                EnsureInitialised();
                return Query("*IDN?");
            }
        }

        public string GetCalibrationStatus()
        {
            lock (thisLock)
            {
                EnsureInitialised();
                CalibrationStatus =
                    Query("CALibrate:VALid?");
                return CalibrationStatus;
            }
        }

        public void ReturnToLocalMode()
        {
            lock (thisLock)
            {
                if (milliKClientSocket.IsConnected())
                {
                    SendCommand("MILLik:LOCal");
                }
            }
        }

        public void Close()
        {
            lock (thisLock)
            {
                if (milliKClientSocket.IsConnected())
                {
                    try
                    {
                        SendCommand("MILLik:LOCal");
                    }
                    catch
                    {
                        // Continue closing the socket even if local-mode
                        // restoration cannot be transmitted.
                    }
                }

                milliKClientSocket.CloseConnection();
                initialised = false;
            }
        }

        private void EnsureInitialised()
        {
            if (!initialised)
                Init();
        }

        private void SendCommand(string command)
        {
            if (!milliKClientSocket.SendData(command))
            {
                initialised = false;

                throw new InvalidOperationException(
                    $"The milliK did not accept the command: {command}");
            }
        }

        private string Query(string command)
        {
            string response = string.Empty;

            if (!milliKClientSocket.SendReceiveData(
                    command,
                    ref response))
            {
                initialised = false;

                throw new InvalidOperationException(
                    $"The milliK did not respond to: {command}");
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new InvalidOperationException(
                    $"The milliK returned an empty response to: " +
                    $"{command}");
            }

            return response.Trim();
        }

        private static double ParseInstrumentReading(
            string response)
        {
            if (double.TryParse(
                    response.Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value))
            {
                if (double.IsNaN(value) ||
                    double.IsInfinity(value))
                {
                    throw new InvalidOperationException(
                        $"The milliK returned a non-finite value: " +
                        $"{response}");
                }

                return value;
            }

            throw new FormatException(
                $"The milliK response is not a valid number: " +
                $"'{response}'.");
        }

        private static string FormatNumber(double value)
        {
            return value.ToString(
                "G17",
                CultureInfo.InvariantCulture);
        }

        private static void ValidateChannel(short channelNumber)
        {
            bool valid = false;
            if((channelNumber >= 10 && channelNumber <= 17) ||
               (channelNumber >= 20 && channelNumber <= 27) ||
               (channelNumber >= 30 && channelNumber <= 37) ||
               (channelNumber >= 40 && channelNumber <= 47))
            {
                valid = true;
            }

            if (!valid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(channelNumber),
                    channelNumber,
                    "Channel must be 10-17, 20-27, " +
                    "30-37 or 40-47.");
            }
        }

        private CorrectionCoefficients GetCorrectionCoefficients(
            short channelNumber)
        {
            if (channelNumber >= 10 && channelNumber <= 17)
            {
                return new CorrectionCoefficients(
                    correctionA1_1,
                    correctionA2_1,
                    correctionA3_1,
                    correctionA4_1);
            }

            if (channelNumber >= 20 && channelNumber <= 27)
            {
                return new CorrectionCoefficients(
                    correctionA1_2,
                    correctionA2_2,
                    correctionA3_2,
                    correctionA4_2);
            }

            if (channelNumber >= 30 && channelNumber <= 37)
            {
                return new CorrectionCoefficients(
                    correctionA1_3,
                    correctionA2_3,
                    correctionA3_3,
                    correctionA4_3);
            }

            return new CorrectionCoefficients(
                correctionA1_4,
                correctionA2_4,
                correctionA3_4,
                correctionA4_4);
        }

        private static double ApplyCorrection(
            double bridgeReading,
            CorrectionCoefficients correction)
        {
            /*
             * Preserves the polynomial from the supplied implementation:
             *
             * corrected =
             *     reading
             *     + A1
             *     + A2 * reading
             *     + A3 * reading^2
             *     + A4 * reading^3
             *
             * Horner form reduces repeated operations.
             */
            return bridgeReading + correction.A1 + bridgeReading * (correction.A2 + bridgeReading * (correction.A3 + correction.A4 * bridgeReading));
        }

        private static double ResistanceToTemperature(
            double resistance,
            PRT probe)
        {
            double a = probe.getA();
            double b = probe.getB();
            double r0 = probe.getR0();


            if (r0 <= 0.0)
            {
                throw new InvalidOperationException(
                    "The PRT R0 coefficient must be greater than zero.");
            }

            /*
             * For t >= 0 degrees C:
             *
             * R(t) = R0 * (1 + A*t + B*t^2)
             *
             * Therefore:
             *
             * B*t^2 + A*t + (1 - R/R0) = 0
             */
            //if (resistance < r0)
            //{
            //    return -1.0; // Temperature is below 0 degrees C.
            //}


            

            double discriminant =
                a * a
                - 4.0 * b * (1.0 - resistance / r0);

            if (discriminant < 0.0)
            {
                throw new ArithmeticException(
                    "The PRT conversion produced a negative " +
                    $"discriminant: {discriminant:G17}.");
            }

            return (-a + Math.Sqrt(discriminant)) /
                   (2.0 * b);
        }


        public void Dispose()
        {
            Close();
            milliKClientSocket.Dispose();
        }

        private readonly struct CorrectionCoefficients
        {
            public CorrectionCoefficients(
                double a1,
                double a2,
                double a3,
                double a4)
            {
                A1 = a1;
                A2 = a2;
                A3 = a3;
                A4 = a4;
            }

            public double A1 { get; }
            public double A2 { get; }
            public double A3 { get; }
            public double A4 { get; }
        }

        public double A1_1
        {
            get => correctionA1_1;
            set => correctionA1_1 = value;
        }

        public double A2_1
        {
            get => correctionA2_1;
            set => correctionA2_1 = value;
        }

        public double A3_1
        {
            get => correctionA3_1;
            set => correctionA3_1 = value;
        }

        public double A4_1
        {
            get => correctionA4_1;
            set => correctionA4_1 = value;
        }

        public double A1_2
        {
            get => correctionA1_2;
            set => correctionA1_2 = value;
        }

        public double A2_2
        {
            get => correctionA2_2;
            set => correctionA2_2 = value;
        }

        public double A3_2
        {
            get => correctionA3_2;
            set => correctionA3_2 = value;
        }

        public double A4_2
        {
            get => correctionA4_2;
            set => correctionA4_2 = value;
        }

        public double A1_3
        {
            get => correctionA1_3;
            set => correctionA1_3 = value;
        }

        public double A2_3
        {
            get => correctionA2_3;
            set => correctionA2_3 = value;
        }

        public double A3_3
        {
            get => correctionA3_3;
            set => correctionA3_3 = value;
        }

        public double A4_3
        {
            get => correctionA4_3;
            set => correctionA4_3 = value;
        }

        public double A1_4
        {
            get => correctionA1_4;
            set => correctionA1_4 = value;
        }

        public double A2_4
        {
            get => correctionA2_4;
            set => correctionA2_4 = value;
        }

        public double A3_4
        {
            get => correctionA3_4;
            set => correctionA3_4 = value;
        }

        public double A4_4
        {
            get => correctionA4_4;
            set => correctionA4_4 = value;
        }
    }
}