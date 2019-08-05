package main

import (
	"os"
	"fmt"
	"io/ioutil"
	"math"
	"path/filepath"
	"strings"
)

const MB = 1048576
const PADDING = 60

type DirectoryInfo struct {
	DirectorySize int64
	Path string
	FileCount int
}

type DirectoryRepository struct { 
	_root string
	_repository map[string]*DirectoryInfo
}

func (repo *DirectoryRepository) GetDirectorySize(path string, recurse bool) (*DirectoryInfo) {
	var directory_size int64
	var number_of_files int

	list_of_items_in_directory, _ := ioutil.ReadDir(path)
	number_of_files = len(list_of_items_in_directory)

	var list_of_subdirectories []string

	for _, item := range list_of_items_in_directory {
		if !item.IsDir() {
			fileInfo, _ := os.Stat(filepath.Join(path,item.Name()))
			directory_size += fileInfo.Size()
		} else {
			list_of_subdirectories = append(list_of_subdirectories, filepath.Join(path,item.Name()))
		}
	}

	if recurse {
		for _, subdirectory := range list_of_subdirectories {
			subDirectoryInfo := repo.GetDirectorySize(subdirectory, true)
			directory_size += subDirectoryInfo.DirectorySize
			number_of_files += subDirectoryInfo.FileCount
		}
	}

	directoryInfo := DirectoryInfo{ Path: path, DirectorySize: directory_size, FileCount: number_of_files }
	return &directoryInfo
}


func (repo *DirectoryRepository) Traverse() {

	repo._repository[repo._root] = repo.GetDirectorySize(repo._root, false)

	list_of_items_in_directory, _ := ioutil.ReadDir(repo._root)	

	var list_of_subdirectories []string
	for _, subdirectory := range list_of_items_in_directory {
		if subdirectory.IsDir() {
			list_of_subdirectories = append(list_of_subdirectories, 
				strings.ToLower(filepath.Join(repo._root,subdirectory.Name())))
		}
	}	
	
	out := make(chan *DirectoryInfo, len(list_of_subdirectories) - 1)
	for _, subdirectory := range list_of_subdirectories {
		go func(path string) {
			directoryInfo := repo.GetDirectorySize(path, true)
			out <- directoryInfo
		}(subdirectory)
	}

	for _, subdirectory := range list_of_subdirectories {
		repo._repository[subdirectory] = <-out 
	}	
}

func (repo *DirectoryRepository) Print() {

	fmt.Println();
	fmt.Printf("%-60s %60s %60s\n", "Directory", "Number of Files", "Size (MB)")

	for _, pathName := range repo._repository {
		fmt.Printf("%-60s %60d %60.1f\n", 
			pathName.Path,
			pathName.FileCount,
			math.Round(float64(pathName.DirectorySize)/MB))
	}

}

func (repo *DirectoryRepository) Initialize(directory string) {
	repo._root = directory
	repo._repository = make(map[string]*DirectoryInfo)
}

func NewDirectoryRepository() *DirectoryRepository {
	return &DirectoryRepository{}
}

func main() {

	/*if len(os.Args) != 2 {
		fmt.Println("You must provide a directory argument at the command line.")
	} else {
		root := os.Args[1]*/
		root := "C:\\tools"
		repo := NewDirectoryRepository()
		repo.Initialize(root)
		repo.Traverse()
		repo.Print()
	//}
}