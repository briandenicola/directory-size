import os
import argparse

def get_dir_size(directory):
	size = 0
	files = [ f for f in os.listdir(directory) if os.path.isfile(os.path.join(directory,f)) ]
	dirs = [ d for d in os.listdir(directory) if os.path.isdir(os.path.join(directory,d)) ]

	for f in files:
		file_path = os.path.join(directory,f)
		size += os.path.getsize(file_path)
		
	for subdir in dirs:
		subdir_path = os.path.join(directory,subdir)
		size += get_dir_size(subdir_path)
	
	return size

MB = 1024 * 1024

parser = argparse.ArgumentParser(description="Get the size of a directory and all its subdirectories.")
parser.add_argument('directory')
args = parser.parse_args()

print "Going to find the size of directory {0}".format(args.directory)

size = 0
files = [ f for f in os.listdir(args.directory) if os.path.isfile(f) ]
dirs = [ d for d in os.listdir(args.directory) if os.path.isdir(d) ]

for f in files :
	file_path = os.path.join(args.directory,f)
	size += os.path.getsize(file_path)
print "{0} - {1:.2f} mb".format(args.directory, float(size)/MB)

size = 0
for d in dirs:
	dir_path = os.path.join(args.directory,d)
	size = get_dir_size(dir_path)
	print "{0} - {1:.2f} mb".format(dir_path, float(size)/MB)