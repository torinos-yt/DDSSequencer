echo "build library : snappy"
cd `dirname $0`
cargo build --target x86_64-pc-windows-gnu --release
echo "copy binary"
cp target/x86_64-pc-windows-gnu/release/snappy.dll ../../Packages/jp.torinos.ddssequencer/Plugin/Windows/