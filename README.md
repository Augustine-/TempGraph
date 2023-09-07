# TempGraph

TempGraph is a real-time visualization of your CPU and GPU temperatures.

Current limitations: 
* Windows-only
* Must be run as Administrator
* I've only tested it on an Intel CPU and Nvidia GPU. There's code in there to check for other GPUs, but they're untested for now.

Instructions: 
* Download
* Run TempGraph.exe as Administrator.
* Observe the cool lines.

Notes:
* Updates once per second. 
* CPU temperatures fluctuate pretty wildly, so I configured it to be a moving average of the last five seconds.

Todos:
* Filter for CPU-only and GPU-only.
* Configuration for a sliding window of time.
* Cap the amount of memory it can use. If left open indefinitely, I'm pretty sure it would eventually eat up all of your memory.
* Make the UI prettier.  

Bugs:
* Slows and becomes unresponsive after remaining open for some time. 
