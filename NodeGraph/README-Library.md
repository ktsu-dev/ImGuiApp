# NodeGraph Built-in Library

A comprehensive programming toolkit for visual node-based development, providing essential operations across multiple domains.

## 📁 **Library Organization**

The node library is organized into logical folders for maintainability and discoverability:

### **📂 Primitives/** - Data Types with Make/Split/Set Pattern
- `NumberTypes.cs` - Number and Integer data types with full lifecycle nodes
- `BooleanTypes.cs` - Boolean data type with full lifecycle nodes
- `StringTypes.cs` - String data type with full lifecycle nodes
- `VectorTypes.cs` - Vector2 data type with full lifecycle nodes

### **📂 Operations/** - Pure Function Node Classes
- `MathOperations.cs` - Basic and advanced mathematical operations
- `StringOperations.cs` - String manipulation and analysis
- `LogicOperations.cs` - Type conversions and logical comparisons
- `CollectionOperations.cs` - Array and collection operations
- `DateTimeOperations.cs` - Date and time manipulations

### **📂 Utilities/** - Specialized Node Classes
- `ControlFlow.cs` - Conditional logic and branching
- `Generators.cs` - Random number and timer nodes
- `Counters.cs` - Counter and accumulator nodes
- `Processors.cs` - Data processing and analysis nodes
- `Geometry.cs` - Geometric calculations and shapes
- `Calculators.cs` - Stateful calculator instances

## 📚 Library Categories

### 🔢 **Data Types (Make/Split/Set Pattern)**
Complete functional data lifecycle with immutable updates:

#### **Make Nodes** (Constructors)
- **Make Number**: Creates NumberData from double value
- **Make Integer**: Creates IntegerData from int value  
- **Make Boolean**: Creates BooleanData from bool value
- **Make String**: Creates StringData from string value
- **Make Vector2**: Creates Vector2Data from X,Y components

#### **Split Nodes** (Accessors)  
- **Split Number**: Extracts value, absolute, conversions from NumberData
- **Split Integer**: Extracts value, even/odd checks, conversions from IntegerData
- **Split Boolean**: Extracts value, NOT operation, conversions from BooleanData
- **Split String**: Extracts value, length, case operations from StringData
- **Split Vector2**: Extracts X, Y, length, normalization from Vector2Data

#### **Set Nodes** (Mutators)
- **Set Number**: Updates NumberData with new double value (immutable)
- **Set Integer**: Updates IntegerData with new int value (immutable)
- **Set Boolean**: Updates BooleanData with new bool value (immutable)
- **Set String**: Updates StringData with new string value (immutable)
- **Set Vector2**: Updates Vector2Data with new X,Y components (immutable)

### 🧮 **Mathematics**

#### **Basic Operations (MathOperations)**
- Add, Subtract, Multiply, Divide, Power
- SquareRoot, Absolute, Min, Max, Clamp
- Sin, Cos, Round

#### **Advanced Mathematics (AdvancedMath)**
- Trigonometry: DegreesToRadians, RadiansToDegrees
- Number Theory: Factorial, GCD, LCM, IsPrime
- Interpolation: Lerp, MapRange
- Utilities: RandomRange

### 📝 **String Processing (StringOperations)**
- Text manipulation: Format, Concatenate, Replace, Substring
- Case conversion: ToUpper, ToLower
- Analysis: Length, Contains
- Validation: Null/empty checking

### 🔄 **Type Conversions (TypeConversions)**
Smart conversion between data types:
- ToString, ToInt, ToDouble, ToBool
- Handles multiple input types with fallbacks

### 🔗 **Logic & Comparisons**

#### **Comparisons**
- Equality: Equal, NotEqual
- Numeric: GreaterThan, LessThan, GreaterOrEqual, LessOrEqual
- Boolean Logic: And, Or, Not

### 📋 **Collections (Collections)**
Array and list operations:
- Access: ArrayLength, ArrayFirst, ArrayLast, ArrayIndexOf
- Search: ArrayContains
- Manipulation: ArraySlice
- Generation: Range, Repeat

### ⏰ **Date & Time (DateTimeOperations)**
Comprehensive time handling:
- Arithmetic: AddDays, AddHours, AddMinutes, DateDifference
- Formatting: FormatDate, ParseDate
- Utilities: IsWeekend, DaysInMonth

### 🎛️ **Utility Classes**

#### **ConditionalNode**
Clean conditional selection between two values.

#### **RandomNode**  
Random number generation:
- Random doubles, integers, booleans, GUIDs

#### **TimerNode**
Time-based operations:
- Current time (UTC/Local), Unix timestamps, elapsed time calculations

#### **CounterNode** 
Stateful counter with increment/decrement operations and status outputs.

#### **ArrayProcessorNode**
Mathematical operations on number arrays:
- Sum, Average, Min, Max, Count

#### **RectangleNode**
Geometric calculations with constructor parameters for width/height.

## 🏗️ **Architecture**

### **Design Principles**
- **Separation of Concerns**: Static methods for pure functions, classes for stateful operations
- **Type Safety**: Strong typing with automatic conversions where appropriate
- **Performance**: Struct nodes for lightweight data, efficient algorithms
- **Extensibility**: Attribute-based system allows easy addition of new nodes

### **Node Types**
1. **Static Method Nodes**: Pure functions (math, string, logic operations)
2. **Make/Split/Set Node Triplets**: Complete functional data lifecycle
3. **Class Nodes**: Stateful operations (timers, counters, processors)

### **Make/Split/Set Pattern Benefits**
- **Complete Data Lifecycle**: Make (construct) → Split (analyze) → Set (update) → repeat
- **Immutable Updates**: Set nodes create new instances rather than mutating existing ones
- **Functional Composition**: Pure data transformations with no side effects
- **Reusability**: Each node type serves a single, clear purpose
- **Type Safety**: Strong typing throughout the entire data manipulation pipeline
- **Performance**: Lightweight structs with efficient copy semantics
- **Debugging**: Clear data flow makes it easy to trace values through the graph

### **Pin System**
- **Automatic Naming**: Meaningful pin names derived from parameters/properties
- **Type Conversions**: Built-in support for common type conversions
- **Instance Chaining**: Output instance pins for fluent operations
- **Smart Defaults**: Reasonable default values for optional parameters

## 🚀 **Usage Examples**

### **Complete Data Lifecycle**
```
Make Number → Split Number → Set Number → Split Number
(Create)     (Analyze)      (Update)     (Final State)
```

### **Functional Data Transformation**
```
Make Vector2 → Split Vector2 → Multiply → Set Vector2 → Split Vector2
(X=1, Y=2)    (Extract X)     (X²=1)     (X=1, Y=2)   (Length=2.24)
```

### **Data Processing Pipeline**
```
Make String → Split String → Set String → Split String → Boolean
(Input)       (Get Length)   (Update)    (New Length)   (IsEmpty)
```

### **Mathematical Data Flow**
```
Make Number → Add → Set Number → Split Number → Format → Make String
(Value A)     (+B)  (A+B)       (Analysis)    (Text)    (Final)
```

### **Conditional Data Updates**
```
Make Boolean → Split Boolean → Conditional → Set Boolean → Split Boolean
(Input)        (Get Value)     (Logic)       (Update)     (Final State)
```

## 🎯 **Use Cases**

- **Game Logic**: Math, vectors, timers, random generation
- **Data Processing**: Collections, arrays, type conversions  
- **UI Logic**: Conditionals, formatting, validation
- **Scientific Computing**: Advanced math, data analysis
- **Automation**: Time-based triggers, counters, state machines
- **Text Processing**: String manipulation, formatting, parsing

## 📦 **Integration**

The library is designed as a comprehensive toolkit that can be extended with domain-specific nodes while providing a solid foundation of essential programming operations.

All nodes support:
- Attribute-based definition
- Automatic pin generation  
- Instance pin chaining
- Rich descriptions and metadata
- Type safety and validation
