import sys
import json
import time
import logging
from datetime import datetime, timedelta
from random import seed
from random import randint
from mqtt.mqtt_wrapper import MQTTWrapper

CHAOS_DATA_TOPIC = "chaossensor/1/data"
SEED = 42

TICK_TOPIC = "tickgen/tick"
def on_message_tick(client, userdata, msg):
    global CHAOS_DATA_TOPIC
    ts_iso = msg.payload.decode("utf-8")
    value = randint(0, 100)
    data = {"payload": value, "timestamp": ts_iso}
    client.publish(CHAOS_DATA_TOPIC, json.dumps(data))

def main():
    seed(SEED)
    mqtt = MQTTWrapper('mqttbroker', 1883, name='chaossensor_1')
    mqtt.subscribe(TICK_TOPIC)
    mqtt.subscribe_with_callback(TICK_TOPIC, on_message_tick)
    
    try:
        mqtt.loop_forever()
    except(KeyboardInterrupt, SystemExit):
        mqtt.stop()
        sys.exit("KeyboardInterrupt -- shutdown gracefully.")

if __name__ == '__main__':
    main()
