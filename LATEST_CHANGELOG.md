## v2.1.5 (patch)

Changes since v2.1.4:

- Implement modern DPI awareness handling in Windows: Updated ForceDpiAware to utilize the latest DPI awareness APIs for better compatibility with windowing libraries. Added fallback mechanisms for older Windows versions and enhanced NativeMethods with new DPI awareness context functions. ([@matt-edmondson](https://github.com/matt-edmondson))
