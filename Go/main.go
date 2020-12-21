package main

import (
	"flag"
	"fmt"
	"os"
	"github.com/briandenicola/directory-size/cmd"
)

func main() {
	var rootPath string 
	
	flag.StringVar(&rootPath, "path", "", "The folder path to check size of")
	flag.Parse()

	if rootPath == "" {
		fmt.Fprintf(os.Stderr, "Missing required -path argument - %s\n", rootPath)
		os.Exit(2)
	}

	repo := cmd.NewDirectoryRepository()
	if err:= repo.Initialize(rootPath); err != nil { 
		fmt.Fprintf(os.Stderr, "Could not open %s or path does not exist\n", rootPath)
		os.Exit(3)
	}
	repo.Execute()
	repo.Print()
}