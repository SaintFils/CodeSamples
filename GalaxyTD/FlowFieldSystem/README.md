The main goal is to constantly update the NativeArray<float2> directions in the InFlowField and OutFlowField components. The systems responsible for unit movement or teleportation takes data from these arrays and uses it as the basis for calculating movement.

The BaseFlowField component stores an array of cells with their costs.

Depending on the size of the map, the calculation of the In and Out FlowField can become quite expensive and time consuming. To ensure stable access to information for the motion system, the process of building the FlowField is divided into 2 systems. The first handles the calculations directly, while the second updates the FlowField components as they are completed.

TeleportationOnHitTag is an example of an effect that uses data from the OutFlowField to find the correct position for teleportation.