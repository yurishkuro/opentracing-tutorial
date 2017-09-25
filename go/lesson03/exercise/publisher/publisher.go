package main

import (
	"log"
	"net/http"
)

func main() {
	http.HandleFunc("/publish", func(w http.ResponseWriter, r *http.Request) {
		helloStr := r.FormValue("helloStr")
		println(helloStr)
	})

	log.Fatal(http.ListenAndServe(":8082", nil))
}
