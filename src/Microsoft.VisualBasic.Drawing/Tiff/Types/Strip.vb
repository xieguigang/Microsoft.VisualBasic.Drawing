' 
'    Copyright 2018 Digimarc, Inc

'    Licensed under the Apache License, Version 2.0 (the "License");
'    you may not use this file except in compliance with the License.
'    You may obtain a copy of the License at

'        http://www.apache.org/licenses/LICENSE-2.0

'    Unless required by applicable law or agreed to in writing, software
'    distributed under the License is distributed on an "AS IS" BASIS,
'    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
'    See the License for the specific language governing permissions and
'    limitations under the License.

'    SPDX-License-Identifier: Apache-2.0


Imports System.Security

Namespace Tiff.Types

    ''' <summary>
    ''' In TIFF (Tagged Image File Format), a **strip** is a fundamental data organization method that
    '''  divides an image into horizontal sections for efficient storage and access. Here’s a concise
    '''  explanation based on authoritative sources:
    ''' 
    '''  ### 1. **Definition and Purpose**  
    ''' 
    '''  A strip is a contiguous block of pixel data representing one or more consecutive rows of an image.
    '''  Instead of storing the entire image as a single unit, TIFF splits it into multiple strips to
    '''  optimize memory usage and I/O operations. This design allows applications to load only specific
    '''  portions of an image (e.g., when editing or viewing a section) without processing the entire
    '''  file.
    ''' 
    '''  ### 2. **Technical Implementation**  
    ''' 
    '''  - **Strip Identification**: Each strip is defined by metadata tags in the TIFF file header:  
    ''' 
    '''    - `StripOffsets`: Specifies the starting byte position of each strip in the file.  
    '''    - `StripByteCounts`: Indicates the size (in bytes) of each strip.  
    ''' 
    '''    These tags enable random access to any strip during reading/writing.  
    ''' 
    '''  - **RowsPerStrip**: A critical tag (`RowsPerStrip`) determines how many rows of pixels each strip
    '''  contains. For example, a 4000-row image with `RowsPerStrip=100` would have 40 strips. Smaller values
    '''  reduce memory overhead but increase metadata size; larger values improve compression efficiency.
    ''' 
    '''  ### 3. **Advantages**  
    ''' 
    '''  - **Memory Efficiency**: Only the required strip(s) are loaded into memory, crucial for large images (e.g., medical scans or satellite photos).  
    '''  - **Partial Processing**: Enables operations like cropping or region-based editing without full-image decompression.  
    '''  - **Compression Flexibility**: Strips can be individually compressed (e.g., using LZW or Deflate), balancing speed and file size.
    ''' 
    '''  ### 4. **Comparison with Tiles**  
    ''' 
    '''  While strips divide images horizontally, **tiles** (another TIFF option) split data into rectangular
    '''  blocks. Strips suit linear workflows (e.g., printing), whereas tiles excel in localized access
    '''  (e.g., GIS applications).
    ''' 
    '''  ### 5. **Practical Considerations**  
    ''' 
    '''  - **Data Alignment**: Strip boundaries must align with pixel data types (e.g., 8-bit data requires `uint8` arrays) to prevent corruption during processing.  
    '''  - **Performance**: Optimal `RowsPerStrip` values depend on use cases—smaller strips for interactive tools, larger for batch processing.
    ''' 
    '''  For deeper technical details, refer to the TIFF 6.0 specification or libraries like `libtiff`/`tifffile`.
    ''' </summary>
    Public Class Strip
        Public Property ImageData As Byte()
        Public Property StripNumber As UShort
        Public Property StripOffset As UInteger

        Public Function GetHash() As String
            Dim md5 = Cryptography.MD5.Create()
            Dim hash = md5.ComputeHash(ImageData)
            Return Convert.ToBase64String(hash)
        End Function
    End Class
End Namespace
