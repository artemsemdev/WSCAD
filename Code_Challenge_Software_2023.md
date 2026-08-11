# WSCAD Code Challenge

## Overview

Your task is to implement a simple vector graphic viewer.

The viewer should read data from a JSON file and display results on the screen in a form of your choice (preferably, but not limited to, a WPF application).

## Input file

Input file is a JSON file consisting of an array of objects, which can be either lines, circles or triangles. You can find example input for your application below.

```json
[
  {
    "type": "line",
    "a": "-1,5; 3,4",
    "b": "2,2; 5,7",
    "color": "127; 255; 255; 255"
  },
  {
    "type": "circle",
    "center": "0; 0",
    "radius": 15.0,
    "filled": false,
    "color": "127; 255; 0; 0"
  },
  {
    "type": "triangle",
    "a": "-15; -20",
    "b": "15; -20,3",
    "c": "0; 21",
    "filled": true,
    "color": "127; 255; 0; 255"
  }
]
```

## Assume the following

1. All coordinates are expressed in Carthesian space with Y axis pointing up (as on paper).
2. Units are virtual. The full picture may exceed size of the screen. In such case it should be proportionally scaled down, so that it will fit the window. If the zoom level is 100%, one unit equals to one pixel.
3. All colors are expressed as ARGB (Alpha, Red, Green, Blue).
4. You may assume, that input data is always valid (you don’t have to perform validation).
5. If `filled` flag is lit, render shape with border and fill. If it is not, render only border. Assume arbitrary border width (eg. 1 unit).

## Extensibility

Put special effort into making sure, that your solution will be extensible. In particular:

1. A new type of primitives may be added in the future (eg. rectangle).
2. A new format for reading data may be introduced (eg. XML).
3. A new behavior of selecting displayed primitives may be required (eg. to inspect their properties like coordinates, colors etc.).

You don’t have to implement it. Be ready though to explain, how much effort would it cost to extend your solution in the described cases.
