use snap::raw::{Decoder, Encoder};
use std::{mem, ptr::null_mut};

#[no_mangle]
pub unsafe extern "C" fn encode_binary(src_data : *const u8, src_size : i32, dst_size : *mut i32) -> *mut u8 {
    let bytes = std::slice::from_raw_parts(src_data, src_size as usize);
    
    let result = Encoder::new().compress_vec(bytes);
    if result.is_err() { return null_mut() }

    let mut dst = result.unwrap();

    *dst_size = dst.len() as i32;
    let ptr = dst.as_mut_ptr();
    mem::forget(dst);

    ptr
}

#[no_mangle]
pub unsafe extern "C" fn decode_binary(src_data : *const u8, src_size : i32, dst_size : *mut i32) -> *mut u8 {
    let bytes = std::slice::from_raw_parts(src_data, src_size as usize);
    
    let result = Decoder::new().decompress_vec(bytes);
    if result.is_err() { return null_mut() }

    let mut dst = result.unwrap();

    *dst_size = dst.len() as i32;

    let ptr = dst.as_mut_ptr();
    mem::forget(dst);

    ptr
}

#[cfg(test)]
mod tests {
    use std::fs::File;
    use std::io::Read;

    use crate::encode_binary;
    use crate::decode_binary;

    #[test]
    fn it_works() {
        let mut file = File::open("img_000592.ddssc").unwrap();
        let mut buf = Vec::new();
        let size = file.read_to_end(&mut buf).unwrap();

        let mut dst_size : i32 = 0;
        let ptr : *mut u8;
        
        println!("original bytes {:?}", size);
        
        unsafe {
            ptr = encode_binary(buf.as_ptr(), size as i32, &mut dst_size);
        
            println!("encoded bytes {:?}", dst_size);
            
            decode_binary(ptr, dst_size as i32, &mut dst_size);

            println!("decoded bytes {:?}", dst_size);
        }

        assert_eq!(size as i32, dst_size);
    }
}
