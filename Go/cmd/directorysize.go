package cmd

import (
	"fmt"
	"github.com/cheggaaa/pb"
	"github.com/olekukonko/tablewriter"
	"io/ioutil"
	"os"
	"path/filepath"
	"sort"
	"strconv"
	"strings"
	"text/tabwriter"
	"time"
)

const MB = 1048576
const PADDING = 60

type DirectoryInfo struct {
	DirectorySize int64
	Path          string
	FileCount     int
}

type DirectoryRepository struct {
	_root          string
	_executionTime time.Duration
	_repository    map[string]*DirectoryInfo
}

func (repo *DirectoryRepository) sorted() []*DirectoryInfo {
	s := make(directorySlice, 0, len(repo._repository))
	for _, d := range repo._repository {
		s = append(s, d)
	}
	sort.Sort(s)
	return s
}

func (repo *DirectoryRepository) convertSizetoString(size int64) string {
	sizeInMB := float64(size) / MB
	return fmt.Sprintf("%.2f", sizeInMB)
}

func (repo *DirectoryRepository) getDirectorySize(path string) *DirectoryInfo {

	var number_of_files int
	var directory_size int64

	_ = filepath.Walk(path, func(path string, info os.FileInfo, err error) error {
		if !info.IsDir() {
			number_of_files++
			directory_size += info.Size()
		}
		return nil
	})

	directoryInfo := DirectoryInfo{Path: path, DirectorySize: directory_size, FileCount: number_of_files}
	return &directoryInfo
}

func (repo *DirectoryRepository) Execute() {

	start := time.Now()

	var number_of_files int
	var directory_size int64

	out := make(chan *DirectoryInfo)

	list_of_items_in_directory, _ := ioutil.ReadDir(repo._root)

	progressTotal := len(list_of_items_in_directory)

	bar := pb.StartNew(progressTotal).SetRefreshRate(time.Millisecond * 10)
	bar.ShowTimeLeft = false
	bar.Start()

	for _, item := range list_of_items_in_directory {
		if item.IsDir() {
			subdirectory := strings.ToLower(filepath.Join(repo._root, item.Name()))

			go func(path string) {
				directoryInfo := repo.getDirectorySize(path)
				out <- directoryInfo
			}(subdirectory)

			bar.Increment()

		} else {
			number_of_files++
			directory_size += item.Size()
		}
	}

	repo._repository[repo._root] = &DirectoryInfo{Path: repo._root, DirectorySize: directory_size, FileCount: number_of_files}
	for _, item := range list_of_items_in_directory {
		if item.IsDir() {
			repo._repository[item.Name()] = <-out
		}
	}

	bar.Set(progressTotal)
	bar.Finish()

	repo._executionTime = time.Since(start)
}

func (repo *DirectoryRepository) Print() {

	w := new(tabwriter.Writer)
	w.Init(os.Stdout, 0, 8, 0, '\t', tabwriter.AlignRight)
	defer w.Flush()

	table := tablewriter.NewWriter(os.Stdout)
	table.SetHeader([]string{"Directory", "Number of Files", "Size (MB)"})

	for _, pathName := range repo.sorted() {
		row := []string{
			pathName.Path,
			strconv.Itoa(pathName.FileCount),
			repo.convertSizetoString(pathName.DirectorySize)}
		table.Append(row)
	}
	
	table.Render()
	fmt.Fprintf(w, "\nTotal Time Taken (ms):\t %d", (repo._executionTime / time.Millisecond))
}

func (repo *DirectoryRepository) Initialize(directory string) error {

	if _, err := os.Stat(directory); err != nil {
		return err
	}

	repo._root = directory
	repo._repository = make(map[string]*DirectoryInfo)

	return nil
}

func NewDirectoryRepository() *DirectoryRepository {
	return &DirectoryRepository{}
}
