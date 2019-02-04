using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using com.thingworx.common.logging;
using com.thingworx.communications.client;
using com.thingworx.communications.client.things;
using com.thingworx.metadata;
using com.thingworx.metadata.annotations;
using com.thingworx.metadata.collections;
using com.thingworx.types;
using com.thingworx.types.collections;
using com.thingworx.types.constants;
using com.thingworx.types.primitives;

// Refer to the "Steam Sensor Example" section of the documentation
// for a detailed explanation of this example's operation 
namespace IoT
{
    // Property Definitions
    [ThingworxPropertyDefinition(name = "Temperature", description = "Current Temperature", baseType = "NUMBER", category = "Status", aspects = new string[] { "isReadOnly:true" })]
    [ThingworxPropertyDefinition(name = "Humidity", description = "Current Humidity", baseType = "NUMBER", category = "Status", aspects = new string[] { "isReadOnly:true" })]
    [ThingworxPropertyDefinition(name = "Timestamp", description = "Current Timestamp", baseType = "DATETIME", category = "Status", aspects = new string[] { "isReadOnly:true" })]
    [ThingworxPropertyDefinition(name = "FaultStatus", description = "Fault status", baseType = "BOOLEAN", category = "Faults", aspects = new string[] { "isReadOnly:true" })]
    [ThingworxPropertyDefinition(name = "InletValve", description = "Inlet valve state", baseType = "BOOLEAN", category = "Status", aspects = new string[] { "isReadOnly:true" })]
    [ThingworxPropertyDefinition(name = "TemperatureLimit", description = "Temperature fault limit", baseType = "NUMBER", category = "Faults", aspects = new string[] { "isReadOnly:false" })]
    [ThingworxPropertyDefinition(name = "TotalFlow", description = "Total flow", baseType = "NUMBER", category = "Aggregates", aspects = new string[] { "isReadOnly:true" })]

    // Event Definitions
    [ThingworxEventDefinition(name = "SteamSensorFault", description = "Steam sensor fault", dataShape = "SteamSensor.Fault", category = "Faults", isInvocable = true, isPropertyEvent = false)]

    // Steam Thing virtual thing class that simulates a Steam Sensor
    public class SteamThing : VirtualThing
    {
        private static readonly TraceSource Logger = LoggerFactory.getLogger(typeof(SteamThing));
        private static string TEMPERATURE_FIELD = "OutsideTemperature";
	    private static string SENSOR_NAME_FIELD = "SensorName";
	    private static string ACTIV_TIME_FIELD = "ActivationTime";
	    private static string PRESSURE_FIELD = "BarometricPressure";
	    private static string FAULT_STATUS_FIELD = "CurrentFaultStatus";
	    private static string INLET_VALVE_FIELD = "CurrentInletValve";
	    private static string TEMPERATURE_LIMIT_FIELD = "RatedTemperatureLimit";
	    private static string TOTAL_FLOW_FIELD = "TotalFlowAmount";

        private double _totalFlow = 0.0;
        // Lock and bool field used to keep the shutdown logic from occuring multiple times before the actual shutdown
        private object _shutdownLock = new object();
        private bool _shuttingDown = false;

        public SteamThing(string name, string description, string identifier, ConnectedThingClient client)
            : base(name, description, identifier, client)
        {
            // Data Shape definition that is used by the steam sensor fault event
            // The event only has one field, the message
            var faultFields = new FieldDefinitionCollection();
            faultFields.addFieldDefinition(new FieldDefinition(CommonPropertyNames.PROP_MESSAGE, BaseTypes.STRING));
            base.defineDataShapeDefinition("SteamSensor.Fault", faultFields);

            // Data shape definition that is used by the GetSteamSensorReadings service
            var readingfields = new FieldDefinitionCollection();
            readingfields.addFieldDefinition(new FieldDefinition(SENSOR_NAME_FIELD, BaseTypes.STRING));
            readingfields.addFieldDefinition(new FieldDefinition(ACTIV_TIME_FIELD, BaseTypes.DATETIME));
            readingfields.addFieldDefinition(new FieldDefinition(TEMPERATURE_FIELD, BaseTypes.NUMBER));
            readingfields.addFieldDefinition(new FieldDefinition(PRESSURE_FIELD, BaseTypes.NUMBER));
            readingfields.addFieldDefinition(new FieldDefinition(FAULT_STATUS_FIELD, BaseTypes.BOOLEAN));
            readingfields.addFieldDefinition(new FieldDefinition(INLET_VALVE_FIELD, BaseTypes.BOOLEAN));
            readingfields.addFieldDefinition(new FieldDefinition(TEMPERATURE_LIMIT_FIELD, BaseTypes.NUMBER));
            readingfields.addFieldDefinition(new FieldDefinition(TOTAL_FLOW_FIELD, BaseTypes.INTEGER));
            defineDataShapeDefinition("SteamSensorReadings", readingfields);

            // Populate the thing shape with the properties, services, and events that are annotated in this code
            base.initializeFromAnnotations();
        }

        // From the VirtualThing class
        // This method will get called when a connect or reconnect happens
        // Need to send the values when this happens
        // This is more important for a solution that does not send its properties on a regular basis
        public override void synchronizeState()
        {
            // Be sure to call the base class
            base.synchronizeState();
            // Send the property values to Thingworx when a synchronization is required
            base.syncProperties();
        }

        // The processScanRequest is called by the SteamSensorClient every scan cycle
        public override void processScanRequest()
        {
            // Be sure to call the base classes scan request
            base.processScanRequest();
            // Execute the code for this simulation every scan
            this.scanDevice();
        }

        // Performs the logic for the steam sensor, occurs every scan cycle
        public void scanDevice()
        {
            var random = new Random();

            // Set the Temperature property value in the range of 400-440
            var temperature = 20 * random.NextDouble();
            // Set the Pressure property value in the range of 18-23 
            var humidity = 18 + 5 * random.NextDouble();
            // Add a random double value from 0.0-1.0 to the total flow
            _totalFlow += random.NextDouble();
            
            // Set the InletValve property value to true by default
            var inletValveStatus = true;

            // If the current second value is divisible by 15, set the InletValve property value to false
            var seconds = DateTime.Now.Second;
            if ((seconds % 15) == 0)
                inletValveStatus = false;

            // Set the property values
            base.setProperty("Temperature", temperature);
            base.setProperty("Timestamp", DateTime.Now);
            base.setProperty("Humidity", humidity);
            base.setProperty("TotalFlow", this._totalFlow);
            base.setProperty("InletValve", inletValveStatus);

            // Get the TemperatureLimmit property value from memory
            var temperatureLimit = (double)getProperty("TemperatureLimit").getValue().getValue();

            // Set the FaultStatus property value if the TemperatureLimit value is exceeded
            // and it is greater than zero
            var faultStatus = temperatureLimit > 0 && temperature > temperatureLimit;

            // If the sensor has a fault...
            if (faultStatus)
            {
                // Get the previous value of the fault from the property
                // This is the current value because it hasn't been set yet
                // This is done because we don't want to send the event every time it enters the fault state, 
                // only send the fault on the transition from non-faulted to faulted
                var previousFaultStatus = (bool)getProperty("FaultStatus").getValue().getValue();

                // If the current value is not faulted, then create and queue the event
                if (!previousFaultStatus)
                {
                    // Set the event information of the defined data shape for the event
                    var eventInfo = new ValueCollection();
                    eventInfo.Add(CommonPropertyNames.PROP_MESSAGE, new StringPrimitive("Temperature at " + temperature + " was above limit of " + temperatureLimit));
                    // Queue the event
                    base.queueEvent("SteamSensorFault", DateTime.UtcNow, eventInfo);
                }
            }

            // Set the fault status property value
            base.setProperty("FaultStatus", faultStatus);

            try {
                // Update the subscribed properties and events to send any updates to Thingworx
                // Without calling these methods, the property and event updates will not be sent
                // The numbers are timeouts in milliseconds.
                base.updateSubscribedProperties(15000);

                LoggerFactory.Log(Logger, TraceEventType.Critical, "Current Temperature limit: {0}", temperatureLimit);
                LoggerFactory.Log(Logger, TraceEventType.Critical,
                    "PUSHED PROPERTY UPDATES \n   " +
                    "Temperature: {0} \n   " +
                    "Humidity: {1} \n   " +
                    "TotalFlow: {2} \n   " +
                    "InletValve :{3} \n   " +
                    "FaultStatus : {4} \n\n",
                    temperature, humidity, _totalFlow, inletValveStatus, faultStatus);

                base.updateSubscribedEvents(6000);
            } catch (Exception ex) {
                // handle exception as appropriate
            }
        }

        [method: ThingworxServiceDefinition(name = "AddNumbers", description = "Add Two Numbers")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "NUMBER")]
        public double AddNumbers(
                [ThingworxServiceParameter(name = "a", description = "Value 1", baseType = "NUMBER")] double a,
                [ThingworxServiceParameter(name = "b", description = "Value 2", baseType = "NUMBER")] double b)
        {
            return a + b;
        }

        [method: ThingworxServiceDefinition(name="GetSteamSensorReadings", description="Get SteamSensor Readings")]
	    [return: ThingworxServiceResult(name=CommonPropertyNames.PROP_RESULT, description="Result", baseType="INFOTABLE", aspects = new string[] { "dataShape:SteamSensorReadings" })]
	    public InfoTable GetSteamSensorReadings() 
	    {		
		    var table = new InfoTable(getDataShapeDefinition("SteamSensorReadings"));
		    var now = DateTime.Now;
		
		    try 
		    {			
			    //entry 1
                var entry = new ValueCollection();
			    entry.SetStringValue(SENSOR_NAME_FIELD, "Sensor Alpha");
			    entry.SetDateTimeValue(ACTIV_TIME_FIELD, now.AddDays(1));
			    entry.SetNumberValue(TEMPERATURE_FIELD, 50);
			    entry.SetNumberValue(PRESSURE_FIELD, 15);
			    entry.SetBooleanValue(FAULT_STATUS_FIELD, false);
			    entry.SetBooleanValue(INLET_VALVE_FIELD, true);
			    entry.SetNumberValue(TEMPERATURE_LIMIT_FIELD, 150);
			    entry.SetNumberValue(TOTAL_FLOW_FIELD, 87);
			    table.addRow(entry);
			
			    //entry 2
                entry = new ValueCollection();
			    entry.SetStringValue(SENSOR_NAME_FIELD, "Sensor Beta");
                entry.SetDateTimeValue(ACTIV_TIME_FIELD, now.AddDays(2));
			    entry.SetNumberValue(TEMPERATURE_FIELD, 60);
			    entry.SetNumberValue(PRESSURE_FIELD, 25);
			    entry.SetBooleanValue(FAULT_STATUS_FIELD, true);
			    entry.SetBooleanValue(INLET_VALVE_FIELD, true);
			    entry.SetNumberValue(TEMPERATURE_LIMIT_FIELD, 150);
			    entry.SetNumberValue(TOTAL_FLOW_FIELD, 77);
			    table.addRow(entry);
			
			    //entry 3
                entry = new ValueCollection();
			    entry.SetStringValue(SENSOR_NAME_FIELD, "Sensor Gamma");
                entry.SetDateTimeValue(ACTIV_TIME_FIELD, now.AddDays(3));
			    entry.SetNumberValue(TEMPERATURE_FIELD, 70);
			    entry.SetNumberValue(PRESSURE_FIELD, 30);
			    entry.SetBooleanValue(FAULT_STATUS_FIELD, true);
			    entry.SetBooleanValue(INLET_VALVE_FIELD, true);
			    entry.SetNumberValue(TEMPERATURE_LIMIT_FIELD, 150);
			    entry.SetNumberValue(TOTAL_FLOW_FIELD, 67);
			    table.addRow(entry);
			
			    //entry 4
                entry = new ValueCollection();
			    entry.SetStringValue(SENSOR_NAME_FIELD, "Sensor Delta");
                entry.SetDateTimeValue(ACTIV_TIME_FIELD, now.AddDays(4));
			    entry.SetNumberValue(TEMPERATURE_FIELD, 80);
			    entry.SetNumberValue(PRESSURE_FIELD, 35);
			    entry.SetBooleanValue(FAULT_STATUS_FIELD, false);
			    entry.SetBooleanValue(INLET_VALVE_FIELD, true);
			    entry.SetNumberValue(TEMPERATURE_LIMIT_FIELD, 150);
			    entry.SetNumberValue(TOTAL_FLOW_FIELD, 57);
			    table.addRow(entry);
			
			    //entry 5
                entry = new ValueCollection();
			    entry.SetStringValue(SENSOR_NAME_FIELD, "Sensor Epsilon");
                entry.SetDateTimeValue(ACTIV_TIME_FIELD, now.AddDays(5));
			    entry.SetNumberValue(TEMPERATURE_FIELD, 90);
			    entry.SetNumberValue(PRESSURE_FIELD, 40);
			    entry.SetBooleanValue(FAULT_STATUS_FIELD, true);
			    entry.SetBooleanValue(INLET_VALVE_FIELD, false);
			    entry.SetNumberValue(TEMPERATURE_LIMIT_FIELD, 150);
			    entry.SetNumberValue(TOTAL_FLOW_FIELD, 47);
			    table.addRow(entry);
		    } 
		    catch (Exception e) 
		    {
                // handle exception as appropriate
		    }

		    return table;
	    }

        [method: ThingworxServiceDefinition(name = "GetBigString", description = "Get big string")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "STRING")]
        public string GetBigString()
        {
            var sbValue = new StringBuilder();

            for (var i = 0; i < 24000; i++)
            {
                sbValue.Append('0');
            }

            return sbValue.ToString();
        }

        [method: ThingworxServiceDefinition(name = "Shutdown", description = "Shutdown the client")]
        [return: ThingworxServiceResult(name=CommonPropertyNames.PROP_RESULT, description="", baseType="NOTHING")]
        public void Shutdown()
        {
            // Highly unlikely that this service could be called more than once, but guard against it anyway
            lock (this._shutdownLock)
            {
                if (!this._shuttingDown)
                {
                    // Start a thread to begin the shutdown or the shutdown could happen before the service returns
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.shutdownThread));
                }
                this._shuttingDown = true;
            }
        }

        private void shutdownThread(object state)
        {
            try
            {
                // Delay for a period to verify that the Shutdown service will return
                Thread.Sleep(1000);
                // Shutdown the client
                this.getClient().shutdown();
            }
            catch
            {
                // Not much can be done if there is an exception here
                // In the case of production code should at least log the error
            }
        }
    }
}
